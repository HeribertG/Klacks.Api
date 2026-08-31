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

    /// <summary>
    /// Why THIS user dismissed THIS notification, kept independently of the ledger row's own RejectReason.
    /// The two are deliberately not the same value. One finding is reported to every planner in its
    /// audience, so several people hold their own dispatch row for the same ConditionId, but Rejected is a
    /// terminal ledger status: the first dismissal wins the compare-and-swap and stamps its reason on the
    /// finding, and every later dismissal loses it and used to have its reason discarded with no trace.
    /// The ledger's value is therefore a sample of one by construction. This column keeps every dismisser's
    /// reason, which is what makes a consensus over a finding computable at all - and it is written even
    /// when the ledger transition was never attempted, because the row carries no ConditionId. Null means
    /// the user gave no reason, never dismissed, or has since replaced the dismissal with another
    /// reaction - the value always describes the reaction currently on the row.
    /// </summary>
    public AgentConditionRejectReason? RejectReason { get; set; }

    public DateTime? ReactionAtUtc { get; set; }

    public DateTime? ReadAtUtc { get; set; }

    /// <summary>
    /// How many reminders went out for this dispatch row after the initial delivery. Drives the
    /// backoff step in ProactiveReminderSchedule - the schedule indexes its ladder by this count.
    /// </summary>
    public int ReminderCount { get; set; }

    /// <summary>
    /// When the next reminder falls due, computed from ProactiveReminderSchedule at delivery and after
    /// every reminder. Null means the row is not scheduled for reminders (acknowledged or opted out).
    /// The reminder sweep picks rows through the partial index on this column.
    /// </summary>
    public DateTime? NextReminderAtUtc { get; set; }

    /// <summary>When the most recent reminder was sent. Null while only the initial dispatch went out.</summary>
    public DateTime? LastRemindedAtUtc { get; set; }

    /// <summary>
    /// When the user acknowledged the notification. This is the ONLY stop truth for the reminder
    /// loop: a row with no acknowledgement keeps being reminded on the backoff schedule, repeating the
    /// last step forever (ProactiveReminderDefaults.RepeatLastStepUntilAcknowledged). Reactions like
    /// dismiss do not stop reminders by themselves.
    /// </summary>
    public DateTime? AcknowledgedAtUtc { get; set; }
}
