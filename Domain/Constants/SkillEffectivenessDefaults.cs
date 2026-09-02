// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Bounds and thresholds of the "Skill-Wirksamkeit" scorecard (W6). Kept in one place so the
/// controller guard, the query default and the aggregation agree on the same numbers.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class SkillEffectivenessDefaults
{
    /// <summary>Time window in days when the caller names none.</summary>
    public const int DefaultDays = 30;

    /// <summary>Shortest accepted window; anything below is a client error.</summary>
    public const int MinDays = 1;

    /// <summary>Longest accepted window; a year is the point where the trend stops being a trend.</summary>
    public const int MaxDays = 365;

    /// <summary>Number of goldset runs shown in the trend table.</summary>
    public const int EvalTrendLimit = 20;

    /// <summary>Trajectory rows sampled for the provenance distribution.</summary>
    public const int TrajectorySampleLimit = 2000;

    /// <summary>Rows in the top and in the flop table.</summary>
    public const int TopFlopLimit = 10;

    /// <summary>A skill needs this many calls in the window before it is ranked at all.</summary>
    public const int TopFlopMinCalls = 5;

    /// <summary>
    /// A skill counts as a flop only below this success rate. Without the threshold the flop table is
    /// the reversed top table, which says nothing when fewer than twice TopFlopLimit skills qualify.
    /// </summary>
    public const double FlopMaxSuccessRate = 0.8;
}
