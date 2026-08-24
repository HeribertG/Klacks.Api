// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Closed catalogue of goal types the reflection pipeline may propose, keyed by the proactive trigger
/// kind each one is derived from. The reflection LLM selects from this catalogue instead of writing
/// free prose, which is what keeps the proposal texts localizable (the frontend renders TitleKey and
/// RationaleKey in the user's UI language, the same way ProactiveMessageI18nKeys works) and keeps
/// technical identifiers out of anything a user reads. CuriosityQuestion and MuteSuggestion have no
/// entry on purpose — TriggerHistoryGoalSignalSource already excludes them as Klacksy's own output.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Constants;

public static class GoalTypeCatalog
{
    private const string TitleKeyPrefix = "assistant-chat.goal-candidates.type.";

    public static readonly IReadOnlyDictionary<string, GoalTypeDefinition> ByTriggerKind =
        new Dictionary<string, GoalTypeDefinition>(StringComparer.Ordinal)
        {
            [AgentTriggerKinds.UnstaffedShift] = Define(
                AgentTriggerKinds.UnstaffedShift,
                "unstaffedShift",
                "shifts in the near future are still without an assigned employee",
                "Staff open shifts earlier",
                "Open shifts came up {0} time(s) in the last {1} days."),

            [AgentTriggerKinds.LockConflict] = Define(
                AgentTriggerKinds.LockConflict,
                "lockConflict",
                "an automated schedule change was refused because the entry was locked against changes",
                "Avoid blocked schedule changes",
                "A blocked schedule change came up {0} time(s) in the last {1} days."),

            [AgentTriggerKinds.TargetHoursDrift] = Define(
                AgentTriggerKinds.TargetHoursDrift,
                "targetHoursDrift",
                "an employee's worked hours drift away from the hours their contract calls for",
                "Reduce deviations from contractual hours",
                "An hour deviation was reported {0} time(s) in the last {1} days."),

            [AgentTriggerKinds.ScenarioPending] = Define(
                AgentTriggerKinds.ScenarioPending,
                "scenarioPending",
                "a schedule variant has been waiting to be accepted or discarded for more than two days",
                "Decide on schedule variants sooner",
                "A waiting schedule variant came up {0} time(s) in the last {1} days."),

            [AgentTriggerKinds.PeriodCloseDue] = Define(
                AgentTriggerKinds.PeriodCloseDue,
                "periodCloseDue",
                "an accounting period is about to end and has not been closed yet",
                "Close accounting periods on time",
                "An upcoming period close came up {0} time(s) in the last {1} days."),

            [AgentTriggerKinds.PeriodOverdue] = Define(
                AgentTriggerKinds.PeriodOverdue,
                "periodOverdue",
                "an accounting period ended some time ago and is still not closed",
                "Work off overdue accounting periods",
                "An overdue period came up {0} time(s) in the last {1} days."),

            [AgentTriggerKinds.ContractExpiringSoon] = Define(
                AgentTriggerKinds.ContractExpiringSoon,
                "contractExpiringSoon",
                "an employment contract runs out soon and no follow-up contract exists yet",
                "Renew expiring contracts earlier",
                "An expiring contract came up {0} time(s) in the last {1} days."),

            [AgentTriggerKinds.SkillSequenceSuggestion] = Define(
                AgentTriggerKinds.SkillSequenceSuggestion,
                "skillSequenceSuggestion",
                "the same two work steps keep being carried out one after the other",
                "Combine recurring work steps",
                "A recurring step sequence came up {0} time(s) in the last {1} days."),

            [AgentTriggerKinds.WorkDroppedByErpImport] = Define(
                AgentTriggerKinds.WorkDroppedByErpImport,
                "workDroppedByErpImport",
                "a planned assignment was cancelled because an order import replaced its order",
                "Keep planned assignments through order imports",
                "A cancelled assignment came up {0} time(s) in the last {1} days."),

            [AgentTriggerKinds.OrderImportFailed] = Define(
                AgentTriggerKinds.OrderImportFailed,
                "orderImportFailed",
                "an order import could not be processed",
                "Get order imports running reliably",
                "A failed order import came up {0} time(s) in the last {1} days."),

            [AgentTriggerKinds.AvailabilityGap] = Define(
                AgentTriggerKinds.AvailabilityGap,
                "availabilityGap",
                "an employee has not entered any availability for the coming month",
                "Close gaps in reported availability",
                "A missing availability came up {0} time(s) in the last {1} days."),

            [AgentTriggerKinds.ClientMissingCoreData] = Define(
                AgentTriggerKinds.ClientMissingCoreData,
                "clientMissingCoreData",
                "an active person record is missing an address or any way to be contacted",
                "Complete missing master data",
                "Missing master data came up {0} time(s) in the last {1} days."),

            [AgentTriggerKinds.PlanPausedForApproval] = Define(
                AgentTriggerKinds.PlanPausedForApproval,
                "planPausedForApproval",
                "a running task stopped halfway because it needs someone to approve the next step",
                "Approve waiting tasks faster",
                "A task waiting for approval came up {0} time(s) in the last {1} days."),

            [AgentTriggerKinds.EscalationStageAlert] = Define(
                AgentTriggerKinds.EscalationStageAlert,
                "escalationStageAlert",
                "an escalation chain had to wake someone up because an absence report was not acknowledged before its deadline",
                "Reduce unacknowledged absence escalations",
                "An escalation wake-up came up {0} time(s) in the last {1} days."),

            [AgentTriggerKinds.OpenOrder] = Define(
                AgentTriggerKinds.OpenOrder,
                "openOrder",
                "an imported or manually created order has not yet been sealed into a staffable shift",
                "Seal open orders earlier",
                "An open order came up {0} time(s) in the last {1} days."),

            [AgentTriggerKinds.UncutFulldayShift] = Define(
                AgentTriggerKinds.UncutFulldayShift,
                "uncutFulldayShift",
                "a full-day duty has not yet been cut into day and night pieces",
                "Cut full-day duties into pieces earlier",
                "An uncut full-day duty came up {0} time(s) in the last {1} days."),

            [AgentTriggerKinds.EmptyContainer] = Define(
                AgentTriggerKinds.EmptyContainer,
                "emptyContainer",
                "a container shift has no slot definitions configured at all",
                "Configure empty container slots earlier",
                "An empty container came up {0} time(s) in the last {1} days.")
        };

    public static GoalTypeDefinition? Find(string? triggerKind) =>
        string.IsNullOrWhiteSpace(triggerKind) ? null
            : ByTriggerKind.TryGetValue(triggerKind, out var definition) ? definition
            : null;

    private static GoalTypeDefinition Define(
        string triggerKind,
        string keySegment,
        string signalDescription,
        string plannerTitle,
        string plannerRationaleFormat) =>
        new(
            triggerKind,
            TitleKeyPrefix + keySegment + ".title",
            TitleKeyPrefix + keySegment + ".rationale",
            signalDescription,
            plannerTitle,
            plannerRationaleFormat);
}
