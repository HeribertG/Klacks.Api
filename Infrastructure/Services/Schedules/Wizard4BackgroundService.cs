// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Klacks.Api.Infrastructure.Services.Schedules;

/// <summary>
/// Hosting shell for the Wizard-4 background anytime-optimiser. Registered only when
/// <c>BackgroundServices:Wizard4</c> is true (default OFF) and pinned to a single API instance.
/// Establishes the safe hosting structure: a periodic tick, a per-tick DI scope, cooperative
/// cancellation, and — critically — the shared <see cref="IHeavyWorkGate"/> so a W4 pass never peaks
/// alongside the ONNX index/embedding work inside the container memory limit (the OOM scar).
///
/// v1 scope: the structure + safety gate are complete and inert by default. The trigger POLICY —
/// detect a user viewing a scenario (IConnectionDateRangeTracker) AND idle (no recent edit on the
/// token), resolve that group's agents, then call <see cref="IWizard4Runner.RunOnceAsync"/> under the
/// gate — is the remaining step. It needs a net-new per-token idle/last-edit signal and must be
/// verified against a running backend with real SignalR presence, so it is intentionally not wired to
/// run blindly here. See docs/wizard4-implementation-plan-2026-06-18.md §Phase-D.
/// </summary>
public sealed class Wizard4BackgroundService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMinutes(2);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHeavyWorkGate _heavyWorkGate;
    private readonly ILogger<Wizard4BackgroundService> _logger;

    public Wizard4BackgroundService(
        IServiceScopeFactory scopeFactory,
        IHeavyWorkGate heavyWorkGate,
        ILogger<Wizard4BackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _heavyWorkGate = heavyWorkGate;
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

    /// <summary>
    /// v1 placeholder for the trigger policy. The functional run path exists
    /// (<see cref="IWizard4Runner.RunOnceAsync"/>); what remains is the decision of WHICH
    /// (group, period, agents) to optimise and WHEN (presence + idle), plus its backend verification.
    /// Until that signal exists this pass is a no-op, so enabling the flag is safe and observable.
    /// </summary>
    private Task RunIdleOptimisationPassAsync(IServiceScope scope, CancellationToken ct)
    {
        _ = scope;
        _ = ct;
        return Task.CompletedTask;
    }
}
