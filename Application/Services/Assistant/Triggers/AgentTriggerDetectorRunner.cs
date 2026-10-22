// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Runs one detector through the proactive pipeline: ledger upsert BEFORE the notification, the STATUS-driven
/// Detected-to-Reported transition and the resolve reconciliation. Extracted from AgentTriggerBackgroundService so
/// an event-driven caller (OpenOrderSummaryRescan after a completed ERP import) goes through exactly the steps of
/// the hourly tick; a caller that only called IAgentTriggerService.OnEventAsync would dispatch without a condition
/// id, and the next tick would dispatch the same finding again WITH one, which the per-recipient dedup cannot
/// recognise as the same message.
/// </summary>
/// <param name="triggerService">Notification pipeline every event is handed to.</param>
/// <param name="ledgerService">Condition ledger: upsert, transition and resolve.</param>
/// <param name="logger">One Information line per detector run with the summed dispatch outcome.</param>

using System.Text.Json;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class AgentTriggerDetectorRunner : IAgentTriggerDetectorRunner
{
    private readonly IAgentTriggerService _triggerService;
    private readonly IAgentConditionLedgerService _ledgerService;
    private readonly ILogger<AgentTriggerDetectorRunner> _logger;

    public AgentTriggerDetectorRunner(
        IAgentTriggerService triggerService,
        IAgentConditionLedgerService ledgerService,
        ILogger<AgentTriggerDetectorRunner> logger)
    {
        _triggerService = triggerService;
        _ledgerService = ledgerService;
        _logger = logger;
    }

    /// <summary>
    /// One detector's findings. Every finding is handed to the notification pipeline on EVERY run it is
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
    ///
    /// Also the one place the dispatch outcome of every event this detector produced is summed and
    /// logged at Information level, one line per detector run - without it the whole pipeline is
    /// invisible whenever the host's default log level sits above Information (Production does), and
    /// AgentTriggerService itself only logs per RECIPIENT, which is far too granular for a host log.
    /// </summary>
    public async Task<AgentTriggerDetectorRunOutcome> RunAsync(
        IAgentTriggerDetector detector,
        CancellationToken cancellationToken = default)
    {
        var events = await detector.DetectAsync(cancellationToken);
        var dispatched = 0;
        var outcome = ProactiveDispatchOutcome.Empty;

        foreach (var triggerEvent in events)
        {
            if (!AgentConditionLedgerPolicy.IsLedgerTracked(triggerEvent))
            {
                outcome = outcome.Add(await _triggerService.OnEventAsync(triggerEvent, cancellationToken));
                dispatched++;
                continue;
            }

            var (condition, _) = await _ledgerService.UpsertDetectedAsync(
                triggerEvent.Kind,
                AgentConditionLedgerPolicy.FingerprintFor(triggerEvent),
                triggerEvent.EntityId,
                AgentConditionLedgerPolicy.LedgerGroupIdsFor(triggerEvent),
                triggerEvent.Severity,
                JsonSerializer.Serialize(triggerEvent.Payload),
                cancellationToken);

            outcome = outcome.Add(await _triggerService.OnEventAsync(triggerEvent, cancellationToken));
            dispatched++;

            if (condition.Status == AgentConditionStatus.Detected)
            {
                await MarkReportedAsync(condition.Id, cancellationToken);
            }
        }

        var resolved = await ReconcileResolvedAsync(detector, cancellationToken);

        _logger.LogInformation(
            "Agent trigger kind {Kind}: {Events} events, {Persisted} persisted, {Throttled} throttled, {Muted} muted, {Deduped} deduped, {Failed} failed, {Resolved} resolved",
            detector.Kind, events.Count, outcome.Persisted, outcome.Throttled, outcome.Muted, outcome.Deduped, outcome.Failed, resolved);

        return new AgentTriggerDetectorRunOutcome(events.Count, dispatched, resolved);
    }

    /// <summary>
    /// Moves a row that is still Detected on to Reported once the notification pipeline has taken it.
    /// Without this step every row would sit in Detected for the rest of its life and the Reported to
    /// Prepared claim the action dispatcher is built around could never fire.
    ///
    /// What Reported does and does not assert: OnEventAsync returned without throwing. The outcome it
    /// returns only counts recipients per gate; it says nothing about whether anybody saw the message.
    /// It applies its own per-user rate limiting, mute settings and audience scoping underneath, so a
    /// row can reach Reported although the event reached nobody - Reported means "handed to the
    /// notification pipeline", never "a human has seen it". A lost compare-and-swap (another instance
    /// got there first) is not an error and is simply left alone.
    /// </summary>
    private async Task MarkReportedAsync(Guid conditionId, CancellationToken cancellationToken)
    {
        await _ledgerService.TryTransitionAsync(
            conditionId,
            AgentConditionStatus.Detected,
            AgentConditionStatus.Reported,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Runs unconditionally, also when the detector returned nothing this run: "everything got fixed"
    /// is precisely the case resolution exists for, and it arrives as an empty event list. Only a
    /// detector that promises a complete fingerprint set may take part - for the others, absence from a
    /// capped scan says nothing about whether the condition is gone, so their rows are left open rather
    /// than resolved on a guess.
    /// </summary>
    private async Task<int> ReconcileResolvedAsync(IAgentTriggerDetector detector, CancellationToken cancellationToken)
    {
        if (detector is not IAgentConditionFingerprintSource fingerprintSource)
        {
            _logger.LogDebug(
                "Agent trigger kind {Kind}: no fingerprint source, resolve reconciliation skipped",
                detector.Kind);

            return 0;
        }

        var activeFingerprints = await fingerprintSource.GetActiveFingerprintsAsync(cancellationToken);

        return await _ledgerService.MarkResolvedAsync(detector.Kind, activeFingerprints, cancellationToken);
    }
}
