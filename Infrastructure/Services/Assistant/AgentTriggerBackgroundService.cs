// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Periodic background service that runs all registered IAgentTriggerDetectors once per hour and turns
/// what they find into a row in the condition ledger (the user-independent memory) plus a notification
/// through IAgentTriggerService, then hands the ledger to the action dispatcher, which is where
/// Klacksy actually remediates something on its own, then runs the reminder sweep, which
/// re-delivers dispatch rows the user never acknowledged, and finally the recipe-run expiry sweep,
/// which retires guided flows the user abandoned. First run is delayed
/// ProactiveHeartbeat.FirstRunDelayMinutes so the application is fully warmed up before scanning.
/// </summary>
/// <param name="scopeFactory">Creates a scoped DI provider per tick.</param>
/// <param name="logger">Structured log per tick.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class AgentTriggerBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AgentTriggerBackgroundService> _logger;

    public AgentTriggerBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<AgentTriggerBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Agent trigger background service started");

        try
        {
            await Task.Delay(TimeSpan.FromMinutes(ProactiveHeartbeat.FirstRunDelayMinutes), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunTickAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Agent trigger scan tick failed");
                }

                await Task.Delay(TimeSpan.FromMinutes(ProactiveHeartbeat.ScanIntervalMinutes), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }

        _logger.LogInformation("Agent trigger background service stopped");
    }

    /// <summary>
    /// One scan over every registered detector. Each detector is isolated: since this tick writes (it did
    /// not before the ledger existed), a single failing detector must not swallow the notifications of
    /// the twelve behind it. Every detector goes through IAgentTriggerDetectorRunner, the same steps an
    /// event-driven rescan (OpenOrderSummaryRescan after an ERP import) uses. Deliberately NOT wrapped in IUnitOfWork.ExecuteInTransactionAsync - the
    /// ledger repository opens its own transaction per transition, and a second, ambient one on the same
    /// scoped context would throw at runtime.
    /// </summary>
    internal async Task RunTickAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var detectors = scope.ServiceProvider.GetServices<IAgentTriggerDetector>().ToList();
        if (detectors.Count == 0)
        {
            return;
        }

        var detectorRunner = scope.ServiceProvider.GetRequiredService<IAgentTriggerDetectorRunner>();

        var totalEvents = 0;
        var totalDispatched = 0;
        var totalResolved = 0;

        foreach (var detector in detectors)
        {
            try
            {
                var outcome = await detectorRunner.RunAsync(detector, cancellationToken);
                totalEvents += outcome.Events;
                totalDispatched += outcome.Dispatched;
                totalResolved += outcome.Resolved;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Agent trigger detector {Kind} failed", detector.Kind);
            }
        }

        await RunActionDispatcherAsync(scope, cancellationToken);

        await RunReminderSweepAsync(scope, cancellationToken);

        await RunRecipeRunExpirySweepAsync(scope, cancellationToken);

        _logger.LogDebug(
            "Agent trigger scan tick complete — {Count} detector(s), {Events} event(s), {Dispatched} dispatched, {Resolved} resolved",
            detectors.Count, totalEvents, totalDispatched, totalResolved);
    }

    /// <summary>
    /// The action branch, run as a SIBLING of the detector loop rather than inside it. It works on the
    /// ledger, not on this tick's findings, so a finding reported in one tick can be acted on in a later
    /// one - and an abandoned claim from a crashed run is resumed here rather than needing a sweep job.
    /// Isolated like a detector: a failing dispatcher must not cost this tick its detection work, which
    /// has already been persisted by the time it runs.
    /// </summary>
    private async Task RunActionDispatcherAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        try
        {
            var actionService = scope.ServiceProvider.GetRequiredService<IAgentConditionActionService>();
            await actionService.RunAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Proactive action dispatcher failed");
        }
    }

    /// <summary>
    /// The reminder branch of package F ("repeat until acknowledged"), run as the LAST sibling step of
    /// the tick - after the detectors and the action dispatcher, so a finding dispatched or executed in
    /// this very tick does not also earn a reminder in it. Isolated like the dispatcher: a failing sweep
    /// must not cost the tick its already persisted detection and action work.
    /// </summary>
    private async Task RunReminderSweepAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        try
        {
            var reminderService = scope.ServiceProvider.GetRequiredService<IProactiveReminderService>();
            var result = await reminderService.RunAsync(cancellationToken);
            _logger.LogDebug(
                "Reminder sweep complete — {Due} due, {Reminded} reminded, {Stopped} stopped, {Skipped} skipped, {Lost} lost",
                result.Due, result.Reminded, result.Stopped, result.Skipped, result.Lost);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Proactive reminder sweep failed");
        }
    }

    /// <summary>
    /// The recipe-run expiry branch (W1.5), the last sibling step of the tick. A guided flow the user
    /// simply walks away from never reaches Completed or Aborted, so without this its row stays
    /// Running and inflates the funnel denominator forever. Isolated like the other siblings: a
    /// failing sweep must not cost the tick its already persisted work.
    /// </summary>
    private async Task RunRecipeRunExpirySweepAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        try
        {
            var sweep = scope.ServiceProvider.GetRequiredService<IRecipeRunExpirySweep>();
            var expired = await sweep.RunAsync(cancellationToken);
            if (expired > 0)
            {
                _logger.LogDebug("Recipe-run expiry sweep complete — {Expired} run(s) expired", expired);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Recipe-run expiry sweep failed");
        }
    }
}
