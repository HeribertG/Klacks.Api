// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Services.Schedules.Recovery;

/// <summary>
/// Constants for building a recovery snapshot from the live plan: the read-context window around the
/// absence (wide enough for the cross-window rest/weekly/consecutive checks) and the start-hour thresholds
/// that classify a work into a coarse shift category for keyword restrictions.
/// </summary>
public static class RecoveryBuildConstants
{
    /// <summary>Days of read context loaded on each side of the absence window (covers the ISO week and rest gaps).</summary>
    public const int ContextWindowDays = 14;

    /// <summary>
    /// Maximum number of cross-group (borrowable) candidates pulled into the snapshot, ordered by Guid.
    /// Bounds the engine's per-demand cost on large root hierarchies; truncation is logged, never silent.
    /// </summary>
    public const int CrossGroupPoolCap = 200;

    /// <summary>A work starting before this hour is classified Early.</summary>
    public const int EarlyBeforeHour = 12;

    /// <summary>A work starting before this hour (and not Early) is classified Late; later starts are Night.</summary>
    public const int LateBeforeHour = 18;
}
