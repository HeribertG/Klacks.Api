// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;

/// <summary>
/// Derives the regression baseline of a turn eval run from the composites of its comparable earlier runs.
/// The median, not the maximum: identical full runs of one model spread by about 0.04, so a maximum turns
/// that noise into regressions.
/// </summary>
public static class TurnEvalBaseline
{
    /// <summary>
    /// Median of the given composites (mean of the two middle values for an even count), or null when
    /// fewer than <see cref="TurnEvalDefaults.MinBaselineRuns"/> are available.
    /// </summary>
    /// <param name="composites">Composites of the comparable completed runs, in any order</param>
    public static decimal? ComputeMedian(IReadOnlyCollection<decimal> composites)
    {
        if (composites.Count < TurnEvalDefaults.MinBaselineRuns)
        {
            return null;
        }

        var sorted = composites.OrderBy(value => value).ToList();
        var middle = sorted.Count / 2;

        return sorted.Count % 2 == 1
            ? sorted[middle]
            : (sorted[middle - 1] + sorted[middle]) / 2;
    }
}
