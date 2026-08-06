// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;
using Klacks.Api.Application.Services.Schedules.AutoWizard;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Infrastructure.Hubs;
using Microsoft.Extensions.DependencyInjection;

namespace Klacks.Api.Infrastructure.Services.Schedules;

/// <summary>
/// Background host for the Wizard-4 anytime-optimiser. Registered only when
/// <c>BackgroundServices:Wizard4</c> is true (default OFF) and pinned to a single API instance.
/// Establishes the full safe hosting structure: a periodic tick, per-tick DI scope, cooperative
/// cancellation, the shared <see cref="IHeavyWorkGate"/> (so a pass never peaks alongside the ONNX
/// index/embedding work — the OOM scar), the presence-based <see cref="Wizard4TriggerPolicy"/>
/// selection and a per-group cooldown.
///
/// Full pass (when enabled): each tick reads who is viewing the Original schedule
/// (<see cref="IConnectionDateRangeTracker"/>), the policy picks the distinct, not-in-cooldown groups,
/// the cooldown is stamped, the group's schedulable employees are resolved
/// (<see cref="IWizard4AgentResolver"/>), and <see cref="IWizard4Runner.RunOnceAsync"/> runs under the
/// gate — emitting a candidate scenario only on a meaningful improvement. Default OFF; the remaining
/// refinement is a true idle signal (beyond the cooldown) and live-backend verification of the
/// presence trigger with real SignalR clients. See docs/wizard4-implementation-plan-2026-06-18.md §Phase-D.
/// </summary>
public sealed class Wizard4BackgroundService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan GroupCooldown = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan RunBudget = TimeSpan.FromSeconds(60);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHeavyWorkGate _heavyWorkGate;
    private readonly IConnectionDateRangeTracker _presence;
    private readonly Wizard4TriggerPolicy _policy = new();
    private readonly Dictionary<Guid, DateTime> _cooldownUntil = new();
    private readonly WizardJobRegistry _wizardJobs;
    private readonly HarmonizerJobRegistry _harmonizerJobs;
    private readonly HolisticHarmonizerJobRegistry _holisticJobs;
    private readonly AutoWizardJobRegistry _autoWizardJobs;
    private readonly ILogger<Wizard4BackgroundService> _logger;

    public Wizard4BackgroundService(
        IServiceScopeFactory scopeFactory,
        IHeavyWorkGate heavyWorkGate,
        IConnectionDateRangeTracker presence,
        WizardJobRegistry wizardJobs,
        HarmonizerJobRegistry harmonizerJobs,
        HolisticHarmonizerJobRegistry holisticJobs,
        AutoWizardJobRegistry autoWizardJobs,
        ILogger<Wizard4BackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _heavyWorkGate = heavyWorkGate;
        _presence = presence;
        _wizardJobs = wizardJobs;
        _harmonizerJobs = harmonizerJobs;
        _holisticJobs = holisticJobs;
        _autoWizardJobs = autoWizardJobs;
        _logger = logger;
    }

    /// <summary>
    /// User-triggered engine runs currently in flight. The optimiser is opt-in and latency-insensitive,
    /// so it yields the machine entirely rather than slowing down a planner who is waiting for a result.
    /// The user runners are deliberately NOT put behind the heavy-work gate - that would block them
    /// behind embedding work instead.
    /// </summary>
    private int UserTriggeredRunCount =>
        _wizardJobs.RunningCount + _harmonizerJobs.RunningCount
        + _holisticJobs.RunningCount + _autoWizardJobs.RunningCount;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Wizard4 background optimiser enabled; tick every {Minutes} min.", TickInterval.TotalMinutes);
        using var timer = new PeriodicTimer(TickInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    var running = UserTriggeredRunCount;
                    if (running > 0)
                    {
                        _logger.LogDebug("Wizard4 tick skipped: {Count} user-triggered engine jobs running", running);
                        continue;
                    }

                    // Never compete with other heavy work for the container memory budget. If busy, skip
                    // this tick rather than queue — the next tick retries.
                    if (!_heavyWorkGate.TryAcquire(out var lease))
                    {
                        continue;
                    }

                    using (lease)
                    {
                        await RunIdleOptimisationPassAsync(stoppingToken);
                    }
                }
                // Only OUR shutdown ends the loop. A cancellation from somewhere else - a timeout token
                // inside a runner, say - is a failed tick, not a reason to stop optimising for good.
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Wizard4 background tick failed");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            // The method has two exit paths - the break above and the outer cancellation - and at the
            // production log level a silent stop is indistinguishable from a service that never ran.
            _logger.LogInformation("Wizard4 background optimiser stopped.");
        }
    }

    private async Task RunIdleOptimisationPassAsync(CancellationToken ct)
    {
        // Before the early returns on purpose: a candidate's time to live must not depend on somebody
        // currently looking at a schedule, or a quiet instance would keep expired candidates forever.
        await ExpireStaleCandidatesAsync(ct);

        var viewed = CollectViewedOriginalGroups();
        if (viewed.Count == 0)
        {
            return;
        }

        var targets = _policy.SelectTargets(viewed, _cooldownUntil, DateTime.UtcNow);
        if (targets.Count == 0)
        {
            return;
        }

        foreach (var target in targets)
        {
            ct.ThrowIfCancellationRequested();
            _cooldownUntil[target.GroupId] = DateTime.UtcNow.Add(GroupCooldown);

            // A scope per target, so one group's DbContext state and one group's failure stay local.
            using var scope = _scopeFactory.CreateScope();

            try
            {
                var running = UserTriggeredRunCount;
                if (running > 0)
                {
                    _logger.LogDebug("Wizard4 pass stopped: {Count} user-triggered engine jobs started", running);
                    return;
                }

                var resolver = scope.ServiceProvider.GetRequiredService<IWizard4AgentResolver>();
                var runner = scope.ServiceProvider.GetRequiredService<IWizard4Runner>();

                var agentIds = await resolver.ResolveAsync(target.GroupId, target.PeriodFrom, target.PeriodUntil, ct);
                if (agentIds.Count == 0)
                {
                    continue;
                }

                var candidate = await runner.RunOnceAsync(target.GroupId, target.PeriodFrom, target.PeriodUntil, agentIds, RunBudget, ct);
                if (candidate is not null)
                {
                    _logger.LogInformation(
                        "Wizard4 created candidate scenario {ScenarioId} for group {GroupId} ({From}..{Until}).",
                        candidate.Id, target.GroupId, target.PeriodFrom, target.PeriodUntil);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Wizard4 pass failed for group {GroupId}", target.GroupId);
            }
        }
    }

    /// <summary>
    /// Removes candidates nobody used within their time to live. Runs on the pass, not on its own
    /// timer: a candidate only matters while someone is looking at that schedule anyway, and a failure
    /// here must not cost the pass its optimisation work.
    /// </summary>
    private async Task ExpireStaleCandidatesAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var lifecycle = scope.ServiceProvider.GetRequiredService<IWizard4CandidateLifecycleService>();
            var expired = await lifecycle.ExpireStaleCandidatesAsync(DateTime.UtcNow, ct);
            if (expired > 0)
            {
                _logger.LogInformation("Wizard4 expired {Count} stale candidate scenarios.", expired);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Wizard4 failed to expire stale candidates");
        }
    }
    /// <summary>Groups currently viewed in the Original (real, AnalyseToken == null) schedule, each with a representative date range.</summary>
    private List<Wizard4TriggerTarget> CollectViewedOriginalGroups()
    {
        var result = new List<Wizard4TriggerTarget>();
        var (_, groupConnections) = _presence.GetConnectionsGroupedBySelectedGroup(null);
        foreach (var (groupId, connectionIds) in groupConnections)
        {
            foreach (var connectionId in connectionIds)
            {
                if (_presence.GetRegisteredDateRange(connectionId) is { } range)
                {
                    result.Add(new Wizard4TriggerTarget(groupId, range.Start, range.End));
                    break;
                }
            }
        }

        return result;
    }
}
