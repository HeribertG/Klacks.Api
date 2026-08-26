// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Periodic background service that runs all registered IAgentTriggerDetectors once per hour and turns
/// what they find into a row in the condition ledger (the user-independent memory) plus a notification
/// through IAgentTriggerService, and then hands the ledger to the action dispatcher, which is where
/// Klacksy actually remediates something on its own. First run is delayed
/// ProactiveHeartbeat.FirstRunDelayMinutes so the application is fully warmed up before scanning.
/// </summary>
/// <param name="scopeFactory">Creates a scoped DI provider per tick.</param>
/// <param name="logger">Structured log per tick.</param>

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Services.Assistant;

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
    /// the twelve behind it. Deliberately NOT wrapped in IUnitOfWork.ExecuteInTransactionAsync - the
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

        var triggerService = scope.ServiceProvider.GetRequiredService<IAgentTriggerService>();
        var ledgerService = scope.ServiceProvider.GetRequiredService<IAgentConditionLedgerService>();

        var totalEvents = 0;
        var totalDispatched = 0;
        var totalResolved = 0;

        foreach (var detector in detectors)
        {
            try
            {
                var outcome = await RunDetectorAsync(detector, triggerService, ledgerService, cancellationToken);
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
    /// One detector's findings. Every finding is handed to the notification pipeline on EVERY tick it is
    /// still observed, and the ledger transition is driven by the row's STATUS - deliberately not by
    /// whether the upsert opened a new row.
    ///
    /// Why the upsert's "is new" flag cannot carry either decision: it is true exactly once per row, so
    /// a recipient the per-user daily rate limiter held back on that one tick would never be offered the
    /// finding again, and a notification pipeline that threw would leave the row on Detected forever -
    /// from where Prepared, the status the action dispatcher claims out of, is not even reachable.
    /// Re-offering costs nothing: AgentTriggerService dedups per recipient on the event's DedupKey, so a
    /// recipient who already has the row is skipped inside it, while a throttled one gets another chance
    /// once the limiter's UTC-midnight reset lands.
    ///
    /// The transition is likewise NOT conditioned on the notification having reached anybody. Mute,
    /// snooze and the per-user rate limit are notification gates; making Reported wait for them would
    /// turn them into action gates, which the governance design forbids - and with a per-user daily
    /// budget of five against a detector cap of fifty findings, it would strand the other forty-five
    /// findings in Detected where no remediation can ever see them.
    /// </summary>
    private async Task<(int Events, int Dispatched, int Resolved)> RunDetectorAsync(
        IAgentTriggerDetector detector,
        IAgentTriggerService triggerService,
        IAgentConditionLedgerService ledgerService,
        CancellationToken cancellationToken)
    {
        var events = await detector.DetectAsync(cancellationToken);
        var dispatched = 0;

        foreach (var triggerEvent in events)
        {
            if (!AgentConditionLedgerPolicy.IsLedgerTracked(triggerEvent))
            {
                await triggerService.OnEventAsync(triggerEvent, cancellationToken);
                dispatched++;
                continue;
            }

            var (condition, _) = await ledgerService.UpsertDetectedAsync(
                triggerEvent.Kind,
                AgentConditionLedgerPolicy.FingerprintFor(triggerEvent),
                triggerEvent.EntityId,
                AgentConditionLedgerPolicy.LedgerGroupIdFor(triggerEvent),
                triggerEvent.Severity,
                JsonSerializer.Serialize(triggerEvent.Payload),
                cancellationToken);

            await triggerService.OnEventAsync(triggerEvent, cancellationToken);
            dispatched++;

            if (condition.Status == AgentConditionStatus.Detected)
            {
                await MarkReportedAsync(ledgerService, condition.Id, cancellationToken);
            }
        }

        var resolved = await ReconcileResolvedAsync(detector, ledgerService, cancellationToken);

        return (events.Count, dispatched, resolved);
    }

    /// <summary>
    /// Moves a row that is still Detected on to Reported once the notification pipeline has taken it.
    /// Without this step every row would sit in Detected for the rest of its life and the Reported to
    /// Prepared claim the action dispatcher is built around could never fire.
    ///
    /// What Reported does and does not assert: OnEventAsync returned without throwing. It returns void
    /// and applies its own per-user rate limiting, mute settings and audience scoping underneath, so a
    /// row can reach Reported although the event reached nobody - Reported means "handed to the
    /// notification pipeline", never "a human has seen it". A lost compare-and-swap (another instance
    /// got there first) is not an error and is simply left alone.
    /// </summary>
    private static async Task MarkReportedAsync(
        IAgentConditionLedgerService ledgerService,
        Guid conditionId,
        CancellationToken cancellationToken)
    {
        await ledgerService.TryTransitionAsync(
            conditionId,
            AgentConditionStatus.Detected,
            AgentConditionStatus.Reported,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Runs unconditionally, also when the detector returned nothing this tick: "everything got fixed"
    /// is precisely the case resolution exists for, and it arrives as an empty event list. Only a
    /// detector that promises a complete fingerprint set may take part - for the others, absence from a
    /// capped scan says nothing about whether the condition is gone, so their rows are left open rather
    /// than resolved on a guess.
    /// </summary>
    private async Task<int> ReconcileResolvedAsync(
        IAgentTriggerDetector detector,
        IAgentConditionLedgerService ledgerService,
        CancellationToken cancellationToken)
    {
        if (detector is not IAgentConditionFingerprintSource fingerprintSource)
        {
            _logger.LogDebug(
                "Agent trigger kind {Kind}: no fingerprint source, resolve reconciliation skipped",
                detector.Kind);

            return 0;
        }

        var activeFingerprints = await fingerprintSource.GetActiveFingerprintsAsync(cancellationToken);

        return await ledgerService.MarkResolvedAsync(detector.Kind, activeFingerprints, cancellationToken);
    }
}
