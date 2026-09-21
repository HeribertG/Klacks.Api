// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// How long the Klacksy proactive ledger keeps finished rows before they are soft-deleted (the physical
/// purge follows later through DataRetentionBackgroundService). Code constants rather than settings: the
/// ledger is operational memory, not user data, and a misconfigured value must not be able to erase the
/// audit trail the action budget and the circuit breaker are counted from.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class AgentLedgerRetentionDefaults
{
    /// <summary>Days a Resolved or Rejected condition is kept after it was resolved or handled.</summary>
    public const int ResolvedOrRejectedConditionDays = 90;

    /// <summary>Days an Executed or Escalated condition is kept; longer because it documents an action Klacksy took.</summary>
    public const int ExecutedOrEscalatedConditionDays = 365;

    /// <summary>Days a dispatch row is kept after its last read, reaction or, when neither exists, its creation.</summary>
    public const int DispatchDays = 180;
}
