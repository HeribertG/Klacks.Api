// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Bounds of the eval trend alert. The threshold is a drop, not a level: the absolute numbers of the
/// turn-selection goldset are low on purpose (it is built out of the turns the assistant got wrong), so
/// an alert on a level would fire every night and mean nothing.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class EvalRegressionDefaults
{
    /// <summary>
    /// Drop in RetrievalHit or SelectionHit between two consecutive full runs that is worth an alert. The
    /// comparison alerts at or beyond this value, not strictly beyond it. The distinction is unreachable for
    /// the 298-item goldset - a drop of (a-b)/298 equals 1/20 only for a-b = 14.9 - but a 300-item goldset
    /// would make it reachable, and alerting on exactly the threshold is the safer side for a regression
    /// signal.
    /// </summary>
    public const double DropThreshold = 0.05;

    /// <summary>
    /// How many recent completed runs one scan reads. The comparison only needs the latest two per model
    /// and scorer version, and one goldset is evaluated nightly, so this covers months of history while
    /// keeping the query a single bounded read.
    /// </summary>
    public const int RecentRunScanLimit = 200;
}
