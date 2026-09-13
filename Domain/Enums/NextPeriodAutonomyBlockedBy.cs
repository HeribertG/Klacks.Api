// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The strongest brake that currently keeps the next-period automation below an unattended commit,
/// in precedence order. It exists because the auto-commit watcher has to name the CAUSE in the blocked
/// event it publishes: a kill switch flipped mid-watch is a different message to a planner than an
/// admin who lowered their level, and both end in the same "still a draft" state.
/// </summary>

namespace Klacks.Api.Domain.Enums;

public enum NextPeriodAutonomyBlockedBy
{
    /// <summary>Nothing brakes: the path may prepare a scenario and commit it.</summary>
    None = 0,

    /// <summary>The global proactive kill switch is set.</summary>
    KillSwitch = 1,

    /// <summary>The governance row of next_period_scheduling_due is switched off.</summary>
    KindDisabled = 2,

    /// <summary>The governance row's effective MaxAction stays below the step the commit needs.</summary>
    MaxAction = 3,

    /// <summary>The admin minimum, capped by the global autonomy level, stays below FullyAutonomous.</summary>
    AutonomyLevel = 4
}
