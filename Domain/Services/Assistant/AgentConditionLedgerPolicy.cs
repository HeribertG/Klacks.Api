// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The two rules that connect the proactive trigger pipeline to the condition ledger: which events
/// become ledger rows, and how a row's fingerprint is spelled. They live here rather than inside the
/// tick because later stages need the same answers - Etappe 3d maps a dismissed notification back to
/// its condition through the identical fingerprint spelling, and Etappe 3f lists findings under the
/// identical tracking rule.
/// </summary>

using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public static class AgentConditionLedgerPolicy
{
    private const string FingerprintSeparator = ":";

    /// <summary>
    /// The ledger remembers world state, not conversations. Two shapes of event are therefore excluded:
    /// one addressed to a single user (TargetUserId), and a companion event, which the pipeline defines
    /// as carrying no audience gate at all (neither PlannersOnly nor AdminOnly) - curiosity and
    /// onboarding style chatter. Both are per-user messages whose DedupKey is not user-distinct, so a
    /// shared ledger row would fold several users into one and the tick's "notify only what is new"
    /// gate would silently swallow every user after the first.
    /// </summary>
    public static bool IsLedgerTracked(IAgentTriggerEvent triggerEvent) =>
        triggerEvent.TargetUserId == null && (triggerEvent.PlannersOnly || triggerEvent.AdminOnly);

    /// <summary>
    /// Build-only, never parsed back apart: both halves are free-form strings. The kind prefix is not
    /// decoration - the unique index on Fingerprint spans all kinds while DedupKeys are only unique
    /// within one (period_close_due and period_overdue spell theirs identically, and four kinds use a
    /// bare entity guid).
    /// </summary>
    public static string FingerprintFor(string triggerKind, string dedupKey) =>
        triggerKind + FingerprintSeparator + dedupKey;

    public static string FingerprintFor(IAgentTriggerEvent triggerEvent) =>
        FingerprintFor(triggerEvent.Kind, triggerEvent.DedupKey);

    /// <summary>
    /// The single group a ledger row records for an event that may concern several. AgentCondition has
    /// one GroupId column, while a shift-borne finding can name two or three groups; the first of the
    /// ordered set is kept as a stable representative. This is a reporting attribute only - the live
    /// audience of a notification is recomputed from the full GroupIds set at dispatch time and is
    /// never read back from this column, so narrowing it here cannot widen anybody's reach.
    /// </summary>
    public static Guid? LedgerGroupIdFor(IAgentTriggerEvent triggerEvent)
    {
        var groupIds = triggerEvent.GroupIds;
        return groupIds.Count > 0 ? groupIds.First() : null;
    }
}
