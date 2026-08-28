// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Stable identifiers for proactive trigger event kinds. Kept here so triggers,
/// rate-limiter and per-user mute settings refer to the same canonical string.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class AgentTriggerKinds
{
    public const string UnstaffedShift = "unstaffed_shift";
    public const string LockConflict = "lock_conflict";
    public const string TargetHoursDrift = "target_hours_drift";
    public const string ScenarioPending = "scenario_pending";
    public const string PeriodCloseDue = "period_close_due";
    public const string ContractExpiringSoon = "contract_expiring_soon";
    public const string SkillSequenceSuggestion = "skill_sequence_suggestion";
    public const string CuriosityQuestion = "curiosity_question";
    public const string WorkDroppedByErpImport = "work_dropped_by_erp_import";
    public const string OrderImportFailed = "order_import_failed";
    public const string AvailabilityGap = "availability_gap";
    public const string PeriodOverdue = "period_overdue";
    public const string ClientMissingCoreData = "client_missing_core_data";
    public const string MuteSuggestion = "mute_suggestion";
    public const string PlanPausedForApproval = "plan_paused_for_approval";
    public const string EscalationStageAlert = "escalation_stage_alert";
    public const string OpenOrder = "open_order";
    public const string UncutFulldayShift = "uncut_fullday_shift";
    public const string EmptyContainer = "empty_container";
    public const string DailyDigest = "daily_digest";
    public const string ScenarioPrepared = "scenario_prepared";
    public const string NextPeriodSchedulingDue = "next_period_scheduling_due";

    /// <summary>
    /// Every kind declared above, in declaration order. AgentTriggerPreferencesController validates an
    /// incoming PUT against this set and lists it on GET, so a kind missing here is a kind nobody can
    /// mute: DismissStreakEvaluator offers a mute for ANY kind dismissed three times in a row, and the
    /// chat's mute button then answered 400. Unlike AgentTriggerGroupScopedKinds and
    /// ProactiveGovernanceDefaults.GovernedKinds this is not a curated subset - it is simply all of them,
    /// written out so the set stays greppable, with AgentTriggerKindsAllGuardTests reflecting over the
    /// consts to turn any drift into a failing test rather than a silent 400.
    ///
    /// escalation_stage_alert is a member even though EscalationNotifier delivers it outside
    /// AgentTriggerService.OnEventAsync and no preference is ever consulted for it: muting it does
    /// nothing, but the endpoint must not reject a kind the user can legitimately name.
    /// </summary>
    public static readonly IReadOnlyList<string> All =
    [
        UnstaffedShift,
        LockConflict,
        TargetHoursDrift,
        ScenarioPending,
        PeriodCloseDue,
        ContractExpiringSoon,
        SkillSequenceSuggestion,
        CuriosityQuestion,
        WorkDroppedByErpImport,
        OrderImportFailed,
        AvailabilityGap,
        PeriodOverdue,
        ClientMissingCoreData,
        MuteSuggestion,
        PlanPausedForApproval,
        EscalationStageAlert,
        OpenOrder,
        UncutFulldayShift,
        EmptyContainer,
        DailyDigest,
        ScenarioPrepared,
        NextPeriodSchedulingDue
    ];
}

public static class AgentTriggerSeverity
{
    public const string High = "high";
    public const string Medium = "medium";
    public const string Low = "low";
}
