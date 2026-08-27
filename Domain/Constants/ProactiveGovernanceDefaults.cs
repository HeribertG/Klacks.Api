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

    /// <summary>
    /// The kind below which a governance row is meaningless: MaxAction steers what happens to a
    /// CONDITION, and only a ledger-tracked event ever becomes one. That is exactly the set matching
    /// AgentConditionLedgerPolicy.IsLedgerTracked - no TargetUserId, and PlannersOnly or AdminOnly set.
    /// Two members carry no detector of their own (order_import_failed, work_dropped_by_erp_import) but
    /// are raised from the ERP import path and do produce ledger rows, so they are governed like the
    /// rest. Per-user companion chatter (curiosity, mute suggestion, plan approval, skill sequence),
    /// the daily digest and the escalation alert are absent on purpose: they never reach the ledger.
    /// ProactiveGovernanceKindGuardTests pins this list against the trigger event classes themselves.
    /// </summary>
    public static readonly IReadOnlyList<string> GovernedKinds = new[]
    {
        AgentTriggerKinds.AvailabilityGap,
        AgentTriggerKinds.ClientMissingCoreData,
        AgentTriggerKinds.ContractExpiringSoon,
        AgentTriggerKinds.EmptyContainer,
        AgentTriggerKinds.LockConflict,
        AgentTriggerKinds.NextPeriodSchedulingDue,
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
}
