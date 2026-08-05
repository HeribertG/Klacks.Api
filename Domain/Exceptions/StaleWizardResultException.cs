// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Exceptions;

/// <summary>
/// Raised when the schedule changed between a wizard run and the attempt to apply its result. Applying
/// anyway would overwrite whatever was added or moved in the meantime, so the caller has to re-run or
/// deliberately repeat the action instead.
/// Deliberately not derived from InvalidOperationException (that maps to 404) nor from ConflictException,
/// so the client can tell a stale result apart from a generic conflict.
/// </summary>
public class StaleWizardResultException : Exception
{
    public StaleWizardResultException(
        string message,
        int expectedWorkCount,
        int actualWorkCount,
        int expectedBreakCount,
        int actualBreakCount,
        bool placementChanged)
        : base(message)
    {
        ExpectedWorkCount = expectedWorkCount;
        ActualWorkCount = actualWorkCount;
        ExpectedBreakCount = expectedBreakCount;
        ActualBreakCount = actualBreakCount;
        PlacementChanged = placementChanged;
    }

    public int ExpectedWorkCount { get; }

    public int ActualWorkCount { get; }

    public int ExpectedBreakCount { get; }

    public int ActualBreakCount { get; }

    /// <summary>True when the counts match but rows moved - the hash caught what the counters cannot.</summary>
    public bool PlacementChanged { get; }
}
