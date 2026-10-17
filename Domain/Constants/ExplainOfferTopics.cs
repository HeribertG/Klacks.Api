// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The topics Klacksy offers to explain first at a gate (a held confirmation or an apply=false preview of
/// a consequential action): which skill's gate it is, which curated happen grounds the explanation, and whether
/// beginners are offered it. A gate belongs to exactly one topic, so the lookup by skill name is unambiguous.
/// The trigger kind of a topic's counter rows (agent_trigger_dispatches) is TriggerKindPrefix + Key; it is the
/// key under which the user's per-kind mute/snooze preference and the offer statistics are kept.
/// </summary>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Constants;

public static class ExplainOfferTopics
{
    public const string TriggerKindPrefix = "explain.topic.";

    public const string ScenarioAcceptKey = "scenario-accept";
    public const string DeleteFinalKey = "delete-final";
    public const string SealingKey = "sealing";
    public const string PlanKey = "plan";
    public const string PlanningCommandsKey = "planning-commands";

    public const string BulkDeleteWorksSkillName = "bulk_delete_works";
    public const string SealOpenOrdersSkillName = "seal_open_orders";
    public const string ApplyPlanningProfileSkillName = "apply_planning_profile";

    public static readonly IReadOnlyList<ExplainOfferTopic> All =
    [
        new(ScenarioAcceptKey, SkillNames.ExplainFinalVsCorrectable, ExplainOfferAudience.All,
            [ScenarioAcceptDefaults.SkillName], []),
        new(DeleteFinalKey, SkillNames.ExplainFinalVsCorrectable, ExplainOfferAudience.All,
            [BulkDeleteWorksSkillName], []),
        new(SealingKey, SkillNames.ExplainShiftLifecycle, ExplainOfferAudience.All,
            [], [SealOpenOrdersSkillName]),
        new(PlanKey, SkillNames.ExplainPlanningWizards, ExplainOfferAudience.AdvancedOnly,
            [PlanSkillDefaults.CreatePlanSkillName], []),
        new(PlanningCommandsKey, SkillNames.ExplainPlanningAssistant, ExplainOfferAudience.AdvancedOnly,
            [ApplyPlanningProfileSkillName], [])
    ];

    /// <summary>The trigger kind of the counter rows of one topic.</summary>
    /// <param name="topic">The topic whose kind is wanted</param>
    public static string TriggerKindOf(ExplainOfferTopic topic) => TriggerKindPrefix + topic.Key;

    /// <summary>True when the trigger kind belongs to an explain offer rather than to a detector notification.</summary>
    /// <param name="triggerKind">Trigger kind of a dispatch row</param>
    public static bool IsOfferKind(string? triggerKind) =>
        triggerKind != null && triggerKind.StartsWith(TriggerKindPrefix, StringComparison.Ordinal);

    /// <summary>The topic of a counter row's trigger kind, null when the kind is no (longer) a known topic.</summary>
    /// <param name="triggerKind">Trigger kind of a dispatch row</param>
    public static ExplainOfferTopic? FindByTriggerKind(string? triggerKind) =>
        All.FirstOrDefault(topic => string.Equals(TriggerKindOf(topic), triggerKind, StringComparison.Ordinal));

    /// <summary>
    /// The topic whose gate is a held confirmation of the given skill, null when none. Only GateSkills count: a preview
    /// skill's gate is its apply=false preview, so a held confirmation of a preview skill (the autonomy gate holding
    /// seal_open_orders) carries no offer.
    /// </summary>
    /// <param name="skillName">Name of the skill whose call returned a Confirmation</param>
    public static ExplainOfferTopic? FindByConfirmationGate(string? skillName) =>
        All.FirstOrDefault(topic => topic.GateSkills.Contains(skillName ?? string.Empty, StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// The confirmation purposes whose held row an accepted offer of this topic may renew: the autonomy gate's
    /// GateReplay for every topic with gate skills, StaleScenarioOverwrite only for the scenario-accept topic (the only
    /// skill that issues it), nothing for a topic without gate skills (its gate is a preview and holds no token).
    /// </summary>
    /// <param name="topic">The accepted topic</param>
    public static IReadOnlyList<string> RenewablePurposesOf(ExplainOfferTopic topic)
    {
        if (topic.GateSkills.Count == 0)
        {
            return [];
        }

        return string.Equals(topic.Key, ScenarioAcceptKey, StringComparison.Ordinal)
            ? [PendingConfirmationPurposes.GateReplay, PendingConfirmationPurposes.StaleScenarioOverwrite]
            : [PendingConfirmationPurposes.GateReplay];
    }

    /// <summary>The topic whose gate is the apply=false preview of the given skill, null when none.</summary>
    /// <param name="skillName">Name of the skill whose preview call succeeded</param>
    public static ExplainOfferTopic? FindByPreviewGate(string? skillName) =>
        All.FirstOrDefault(topic => topic.PreviewGateSkills.Contains(skillName ?? string.Empty, StringComparer.OrdinalIgnoreCase));
}
