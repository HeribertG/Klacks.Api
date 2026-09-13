// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Why the FullyAutonomous branch did not accept an autofill scenario into the real schedule. Every
/// value is a visible outcome: each one posts a NextPeriodAutoCommitBlockedTriggerEvent with its own
/// wording, because "the plan is still a draft" is the same end state for all of them but the reason a
/// human has to act on is not. The value is part of the event's DedupKey, so two different reasons for
/// the same group and period are two notifications rather than one that swallows the other.
/// </summary>

namespace Klacks.Api.Domain.Enums;

public enum NextPeriodAutoCommitBlockReason
{
    /// <summary>The scenario introduces at least one new compliance issue.</summary>
    NewViolations = 0,

    /// <summary>The accept pipeline returned false instead of accepting.</summary>
    Refused = 1,

    /// <summary>The accept gate refused with a conflict (Block-mode compliance, concurrent changes).</summary>
    Conflict = 2,

    /// <summary>The watched wizard chain did not finish inside the watch window.</summary>
    Timeout = 3,

    /// <summary>The chain ended without a committable result (failed, cancelled, no final scenario).</summary>
    NotCommittable = 4,

    /// <summary>The global proactive kill switch was flipped while the chain was being watched.</summary>
    KillSwitch = 5,

    /// <summary>The effective autonomy level dropped below FullyAutonomous while the chain was being watched.</summary>
    AutonomyLowered = 6,

    /// <summary>The watcher itself is gone (API restart) while its scenario is still an unaccepted draft.</summary>
    Interrupted = 7
}
