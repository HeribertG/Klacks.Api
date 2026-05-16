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
}

public static class AgentTriggerSeverity
{
    public const string High = "high";
    public const string Medium = "medium";
    public const string Low = "low";
}
