// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
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
/// What runs today (observable when enabled): each tick reads who is viewing the Original schedule
/// (<see cref="IConnectionDateRangeTracker"/>), the policy picks the distinct, not-in-cooldown groups,
/// and the cooldown is stamped. What remains (the final wiring, gated off so this is safe): resolving a
/// group's schedulable employees and invoking <see cref="IWizard4Runner.RunOnceAsync"/> under the gate
/// — that needs a group→employee resolution + live-backend verification (real SignalR presence and a
/// true idle signal). See docs/wizard4-implementation-plan-2026-06-18.md §Phase-D.
/// </summary>
public sealed class Wizard4BackgroundService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan GroupCooldown = TimeSpan.FromMinutes(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHeavyWorkGate _heavyWorkGate;
    private readonly IConnectionDateRangeTracker _presence;
    private readonly Wizard4TriggerPolicy _policy = new();
    private readonly Dictionary<Guid, DateTime> _cooldownUntil = new();
    private readonly ILogger<Wizard4BackgroundService> _logger;

    public Wizard4BackgroundService(
        IServiceScopeFactory scopeFactory,
        IHeavyWorkGate heavyWorkGate,
        IConnectionDateRangeTracker presence,
        ILogger<Wizard4BackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _heavyWorkGate = heavyWorkGate;
        _presence = presence;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Wizard4 background optimiser enabled; tick every {Minutes} min.", TickInterval.TotalMinutes);
        using var timer = new PeriodicTimer(TickInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                // Never compete with other heavy work for the container memory budget. If busy, skip
                // this tick rather than queue — the next tick retries.
                if (!_heavyWorkGate.TryAcquire(out var lease))
                {
                    continue;
                }

                using (lease)
                using (var scope = _scopeFactory.CreateScope())
                {
                    await RunIdleOptimisationPassAsync(scope, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Wizard4 background tick failed");
            }
        }
    }

    private Task RunIdleOptimisationPassAsync(IServiceScope scope, CancellationToken ct)
    {
        _ = scope;
        var viewed = CollectViewedOriginalGroups();
        if (viewed.Count == 0)
        {
            return Task.CompletedTask;
        }

        var targets = _policy.SelectTargets(viewed, _cooldownUntil, DateTime.UtcNow);
        foreach (var target in targets)
        {
            _cooldownUntil[target.GroupId] = DateTime.UtcNow.Add(GroupCooldown);

            // FINAL WIRING (gated off): resolve target.GroupId -> schedulable employee client ids, then
            //   var runner = scope.ServiceProvider.GetRequiredService<IWizard4Runner>();
            //   await runner.RunOnceAsync(target.GroupId, target.PeriodFrom, target.PeriodUntil, agentIds, budget, ct);
            // Left unwired pending group->employee resolution + live-backend verification; logging the
            // intent keeps the trigger observable when the flag is on without running unverified work.
            _logger.LogInformation(
                "Wizard4 would optimise group {GroupId} for {From}..{Until} (run wiring pending).",
                target.GroupId, target.PeriodFrom, target.PeriodUntil);
        }

        return Task.CompletedTask;
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
