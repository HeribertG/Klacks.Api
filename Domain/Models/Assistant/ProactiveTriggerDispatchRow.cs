// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Records that a proactive trigger with a given content key was already dispatched to a user, so the
/// same alert is never sent twice (survives restarts). Keyed by (UserId, TriggerKind, DedupKey).
/// Also stores what was delivered (ContentKey, ContentParamsJson, Severity) and the user's reaction
/// (helpful / dismissed) so trigger quality becomes measurable per kind.
/// </summary>

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant;

public class ProactiveTriggerDispatchRow : BaseEntity
{
    public string UserId { get; set; } = string.Empty;

    public string TriggerKind { get; set; } = string.Empty;

    public string DedupKey { get; set; } = string.Empty;

    public string? ContentKey { get; set; }

    public string? ContentParamsJson { get; set; }

    public string? Severity { get; set; }

    public string? ActionRoute { get; set; }

    public string? ActionParamsJson { get; set; }

    /// <summary>
    /// The condition-ledger row this notification reported, when the event took part in the ledger at
    /// all. Null is the normal case, not an anomaly: only detector events that
    /// AgentConditionLedgerPolicy.IsLedgerTracked admits ever open a ledger row, so companion
    /// broadcasts, per-user events and everything posted outside the trigger tick stay unlinked. Set at
    /// dispatch time by matching Kind + DedupKey through AgentConditionLedgerPolicy.FingerprintFor,
    /// which is what lets a later dismissal write its reject reason back onto the finding.
    /// </summary>
    public Guid? ConditionId { get; set; }

    public ProactiveReaction Reaction { get; set; } = ProactiveReaction.None;

    public DateTime? ReactionAtUtc { get; set; }

    public DateTime? ReadAtUtc { get; set; }
}
