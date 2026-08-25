// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// What came of laying a prepared scenario in front of a human for one condition. Everything except
/// Prepared leaves the world untouched, so a caller may simply skip and try the row again next tick.
/// </summary>
public enum ConditionScenarioPreparationOutcome
{
    /// <summary>A scenario exists, the ledger row points at it and is now Prepared.</summary>
    Prepared = 0,

    /// <summary>The row was already Prepared; the existing scenario is reported instead of a second one.</summary>
    AlreadyPrepared = 1,

    /// <summary>
    /// The row is in a status from which Prepared is unreachable - still Detected (nobody was told yet),
    /// or already terminal. Nothing was created.
    /// </summary>
    NotPreparable = 2,

    /// <summary>
    /// Another instance moved the row between reading its status and the compare-and-swap. The scenario
    /// this call had already created is discarded again, so the winner's scenario stays the only one.
    /// </summary>
    LedgerConflict = 3
}
