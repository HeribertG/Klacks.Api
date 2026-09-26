// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Why a period that Klacksy was allowed to close on its own was NOT closed. Every value is reported as a
/// PeriodAutoCloseBlockedTriggerEvent and is part of its DedupKey, so each cause is told once per group and
/// period instead of on every scan, and a later, different cause is not swallowed by an earlier one.
/// </summary>

namespace Klacks.Api.Domain.Enums;

public enum PeriodAutoCloseBlockReason
{
    /// <summary>No PERIOD_CLOSE_LAG_DAYS is stored - nobody was asked when periods are closed.</summary>
    NoLagStored = 0,

    /// <summary>The close date lies further back than PeriodAutoClose.WindowDays; a person has to close it.</summary>
    CloseWindowMissed = 1,

    /// <summary>Some days of the period are sealed already, but not its last day.</summary>
    PartiallySealed = 2,

    /// <summary>The period still holds errors (list_period_issues); a seal is never acknowledged automatically.</summary>
    OpenErrors = 3,

    /// <summary>The autonomy gate closed between the evaluation and the seal.</summary>
    AutonomyLowered = 4,

    /// <summary>The close handler refused (permission or validation).</summary>
    Refused = 5,

    /// <summary>The close failed with an unexpected error.</summary>
    Failed = 6,

    /// <summary>The seal ran but the fresh read-back does not show every day of the period sealed.</summary>
    NotVerified = 7
}
