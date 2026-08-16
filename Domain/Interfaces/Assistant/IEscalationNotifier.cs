// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The escalation chain's own narrow delivery path (docs/ENTWURF-eskalationskette-2026-08-16.md §2).
/// Reuses the building blocks below AgentTriggerService.OnEventAsync directly and deliberately skips
/// that method itself, because its mute/daily-budget/dedup gates would silently swallow an
/// escalation stage the same way they would any other proactive trigger (decision B5).
/// </summary>

using Klacks.Api.Domain.Models.Assistant.Escalation;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IEscalationNotifier
{
    /// <summary>
    /// Writes the inbox row, live-pushes if the recipient is connected, and ALWAYS also attempts the
    /// messenger (Owner decision A1: a connected-but-unattended tab must not count as reached).
    /// </summary>
    Task<EscalationNotificationResult> NotifyStageAsync(
        EscalationChain chain, EscalationStage stage, DateTime dueAtUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms the acknowledgement back to the responder and leaves a quiet, inbox-only note (no
    /// live push, no messenger) for every stage that was already notified before them, so nobody who
    /// was asked earlier double-acts in the morning.
    /// </summary>
    Task NotifyHandoffAsync(
        EscalationChain chain,
        EscalationStage acknowledgedStage,
        IReadOnlyList<EscalationStage> previouslyNotifiedStages,
        CancellationToken cancellationToken = default);
}
