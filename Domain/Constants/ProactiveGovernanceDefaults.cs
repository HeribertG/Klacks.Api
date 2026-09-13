// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The fail-safe values a governance rule falls back to and the set of trigger kinds a rule can govern.
/// A kind with no row behaves exactly like a row holding these defaults, so a kind added in a later
/// stage can never land in an undefined state - it reports and waits, which is what the pipeline did
/// before governance existed.
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Constants;

public static class ProactiveGovernanceDefaults
{
    public const ProactiveMaxAction MaxAction = ProactiveMaxAction.Hint;
    public const bool Enabled = true;
    public const int DailyActionBudget = 5;
    public const int WindowActionLimit = 3;
    public const int WindowMinutes = 60;

    /// <summary>Fail-safe global autonomy level: report and wait, exactly like MaxAction above.</summary>
    public const AutonomyLevel GlobalAutonomyLevel = AutonomyLevel.Propose;

    /// <summary>
    /// The global-level to ProactiveMaxAction ladder: 0 reports only, 1 additionally stages a scenario,
    /// 2 and 3 both execute - level 3 is reserved for a future class of auto-commit kinds that Execute
    /// does not yet distinguish, so it maps to the same cap as 2 rather than to an action that does not
    /// exist.
    /// </summary>
    public static ProactiveMaxAction MapAutonomyLevel(AutonomyLevel level) => level switch
    {
        AutonomyLevel.Propose => ProactiveMaxAction.Hint,
        AutonomyLevel.Assisted => ProactiveMaxAction.Prepare,
        AutonomyLevel.Autonomous => ProactiveMaxAction.Execute,
        AutonomyLevel.FullyAutonomous => ProactiveMaxAction.Execute,
        _ => ProactiveMaxAction.Hint
    };

    /// <summary>
    /// The kind below which a governance row is meaningless: MaxAction steers what happens to a
    /// CONDITION, and only a ledger-tracked event ever becomes one. That is exactly the set matching
    /// AgentConditionLedgerPolicy.IsLedgerTracked - no TargetUserId, and PlannersOnly or AdminOnly set.
    /// Two members carry no detector of their own (order_import_failed, work_dropped_by_erp_import) and
    /// are raised directly via IAgentTriggerService.OnEventAsync from the ERP import path
    /// (ErpOrderImportRunner, OrderSupersessionService), never through UpsertDetectedAsync - so they are
    /// governed (preferences, budget, kill switch all apply) but never open a ledger row: no reminder
    /// loop, no resolve reconciliation, no action dispatcher. Per-user companion chatter (curiosity, mute
    /// suggestion, plan approval, skill sequence), the daily digest and the escalation alert are absent on
    /// purpose: they never reach the ledger either. ProactiveGovernanceKindGuardTests pins this list
    /// against the trigger event classes themselves.
    /// </summary>
    public static readonly IReadOnlyList<string> GovernedKinds = new[]
    {
        AgentTriggerKinds.AvailabilityGap,
        AgentTriggerKinds.ClientMissingCoreData,
        AgentTriggerKinds.ContractExpiringSoon,
        AgentTriggerKinds.EmptyContainer,
        AgentTriggerKinds.EvalRegression,
        AgentTriggerKinds.KlacksyLearnedDigest,
        AgentTriggerKinds.LockConflict,
        AgentTriggerKinds.NextPeriodSchedulingDue,
        AgentTriggerKinds.NoScheduleYet,
        AgentTriggerKinds.OpenOrder,
        AgentTriggerKinds.OrderImportFailed,
        AgentTriggerKinds.PeriodCloseDue,
        AgentTriggerKinds.PeriodOverdue,
        AgentTriggerKinds.ScenarioPending,
        AgentTriggerKinds.TargetHoursDrift,
        AgentTriggerKinds.UncutFulldayShift,
        AgentTriggerKinds.UnstaffedShift,
        AgentTriggerKinds.WorkDroppedByErpImport
    };

    public static bool IsGovernedKind(string triggerKind) =>
        GovernedKinds.Contains(triggerKind, StringComparer.Ordinal);

    /// <summary>
    /// Per-kind exceptions to <see cref="MaxAction"/> for the seeded default row. The next-period
    /// autofill is the one governed kind whose action path was built and reviewed specifically to run
    /// unattended (see INextPeriodAutonomyResolver); seeding it at the global Hint default would leave
    /// every installation silently unable to autofill, even where the global autonomy level and the
    /// admins' own preferences already allow it, and nobody would know why. Every other kind keeps the
    /// fail-safe Hint default. This only sets the CEILING an admin can raise a rule to - the global
    /// level, admin preferences, the kill switch and Enabled remain the actual brakes.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, ProactiveMaxAction> SeededMaxActionOverrides =
        new Dictionary<string, ProactiveMaxAction>(StringComparer.Ordinal)
        {
            [AgentTriggerKinds.NextPeriodSchedulingDue] = ProactiveMaxAction.Execute
        };

    /// <summary>The seeded max action for one kind: its override if one exists, otherwise <see cref="MaxAction"/>.</summary>
    public static ProactiveMaxAction SeededMaxActionFor(string triggerKind) =>
        SeededMaxActionOverrides.TryGetValue(triggerKind, out var overrideAction) ? overrideAction : MaxAction;
}
