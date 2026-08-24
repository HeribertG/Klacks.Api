// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Periodic background service that runs all registered IAgentTriggerDetectors once per hour and turns
/// what they find into two things: a row in the condition ledger (the user-independent memory) and, only
/// for a condition seen for the first time, a notification through IAgentTriggerService. First run is
/// delayed 2 minutes so the application is fully warmed up before scanning.
/// </summary>
/// <param name="scopeFactory">Creates a scoped DI provider per tick.</param>
/// <param name="logger">Structured log per tick.</param>

using System.Text.Json;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class AgentTriggerBackgroundService : BackgroundService
{
    private const int ScanIntervalMinutes = 60;
    private const int FirstRunDelayMinutes = 2;

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
            await Task.Delay(TimeSpan.FromMinutes(FirstRunDelayMinutes), stoppingToken);

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

                await Task.Delay(TimeSpan.FromMinutes(ScanIntervalMinutes), stoppingToken);
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
        var totalNotified = 0;
        var totalResolved = 0;

        foreach (var detector in detectors)
        {
            try
            {
                var outcome = await RunDetectorAsync(detector, triggerService, ledgerService, cancellationToken);
                totalEvents += outcome.Events;
                totalNotified += outcome.Notified;
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

        _logger.LogDebug(
            "Agent trigger scan tick complete — {Count} detector(s), {Events} event(s), {Notified} notified, {Resolved} resolved",
            detectors.Count, totalEvents, totalNotified, totalResolved);
    }

    private async Task<(int Events, int Notified, int Resolved)> RunDetectorAsync(
        IAgentTriggerDetector detector,
        IAgentTriggerService triggerService,
        IAgentConditionLedgerService ledgerService,
        CancellationToken cancellationToken)
    {
        var events = await detector.DetectAsync(cancellationToken);
        var notified = 0;

        foreach (var triggerEvent in events)
        {
            if (!AgentConditionLedgerPolicy.IsLedgerTracked(triggerEvent))
            {
                await triggerService.OnEventAsync(triggerEvent, cancellationToken);
                notified++;
                continue;
            }

            var (condition, isNew) = await ledgerService.UpsertDetectedAsync(
                triggerEvent.Kind,
                AgentConditionLedgerPolicy.FingerprintFor(triggerEvent),
                triggerEvent.EntityId,
                AgentConditionLedgerPolicy.LedgerGroupIdFor(triggerEvent),
                triggerEvent.Severity,
                JsonSerializer.Serialize(triggerEvent.Payload),
                cancellationToken);

            if (!isNew)
            {
                continue;
            }

            await triggerService.OnEventAsync(triggerEvent, cancellationToken);
            notified++;

            await MarkReportedAsync(ledgerService, condition.Id, cancellationToken);
        }

        var resolved = await ReconcileResolvedAsync(detector, ledgerService, cancellationToken);

        return (events.Count, notified, resolved);
    }

    /// <summary>
    /// Moves a freshly opened row from Detected to Reported once the notification pipeline accepted it.
    /// Without this step every row would sit in Detected for the rest of its life and the Reported to
    /// Prepared claim later stages are specified around could never fire.
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
