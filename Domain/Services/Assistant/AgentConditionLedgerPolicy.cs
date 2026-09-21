// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The rules that connect the proactive trigger pipeline to the condition ledger: which events become
/// ledger rows, how a row's fingerprint is spelled, and which groups a row records - the whole set it
/// concerns and the single primary one among them. They live here rather than inside the tick because
/// later stages need the same answers - Etappe 3d maps a dismissed notification back to its condition
/// through the identical fingerprint spelling, and Etappe 3f lists findings under the identical tracking
/// rule.
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
    /// Every group a ledger row concerns, deduplicated. This - not
    /// <see cref="PrimaryGroupIdFor(IReadOnlySet{Guid})"/> - is what the row's visibility is decided on: a
    /// shift-borne finding can name two or three groups, and the planner-facing reads admit it for a
    /// planner scoped to ANY of them. Deduplicated because the set is persisted into
    /// agent_condition_groups, whose composite key would reject a repeated pair, and because a duplicate
    /// would be meaningless anyway.
    /// </summary>
    public static IReadOnlySet<Guid> LedgerGroupIdsFor(IAgentTriggerEvent triggerEvent) =>
        triggerEvent.GroupIds.ToHashSet();

    /// <summary>
    /// The row's PRIMARY group - the single value AgentCondition.GroupId keeps for the callers that need
    /// exactly one: the per-group action budget and the governance lookup are counted in it, so the pick
    /// must be stable across ticks and re-arms, which is why it is the smallest id rather than the first
    /// one the event happens to enumerate. It is NOT the row's audience; see
    /// <see cref="LedgerGroupIdsFor"/>. Null exactly when the set is empty, which is the invariant the
    /// scope reads use to tell "concerns no group at all" apart from "concerns groups" without joining.
    /// </summary>
    public static Guid? PrimaryGroupIdFor(IReadOnlySet<Guid> groupIds) =>
        groupIds.Count > 0 ? groupIds.Min() : null;
}
