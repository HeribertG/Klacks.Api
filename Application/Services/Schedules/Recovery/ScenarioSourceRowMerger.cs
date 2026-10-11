// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Merges the real plan with a source scenario the way the scenario clone does. On the days of the source range the
/// source scenario replaces the whole plan of the employees it covers (its group's members, or everybody when it has no
/// group). For every other employee ownership is per shift: their real plan stays (home-group works, absences), only
/// their real rows on shifts the source scenario cloned are replaced by the source scenario's rows on those shifts, so a
/// borrowed employee keeps the home commitments and holidays and nothing is counted twice. Outside the range everything
/// is the real plan.
/// </summary>

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Services.Schedules.Recovery;

public static class ScenarioSourceRowMerger
{
    /// <param name="realRows">Rows of the real plan</param>
    /// <param name="scenarioRows">Rows of the source scenario</param>
    /// <param name="scope">Source scenario range, group and owned shifts</param>
    /// <param name="coveredClientIds">Employees whose whole plan the source scenario owns; null = everybody</param>
    /// <param name="clientOf">Employee of a row</param>
    /// <param name="dateOf">Calendar day of a row</param>
    /// <param name="isOnOwnedShift">Whether a row belongs to a shift the source scenario owns (false for rows without a shift)</param>
    public static List<T> Merge<T>(
        IReadOnlyList<T> realRows,
        IReadOnlyList<T> scenarioRows,
        ScenarioSourceScope scope,
        IReadOnlyCollection<Guid>? coveredClientIds,
        Func<T, Guid> clientOf,
        Func<T, DateOnly> dateOf,
        Func<T, bool> isOnOwnedShift)
    {
        var covered = coveredClientIds?.ToHashSet();
        bool IsCovered(T row) => covered is null || covered.Contains(clientOf(row));

        bool RealRowStays(T row)
            => !scope.OwnsDate(dateOf(row)) || (!IsCovered(row) && !isOnOwnedShift(row));

        bool ScenarioRowCounts(T row)
            => scope.OwnsDate(dateOf(row)) && (IsCovered(row) || isOnOwnedShift(row));

        return realRows.Where(RealRowStays)
            .Concat(scenarioRows.Where(ScenarioRowCounts))
            .ToList();
    }
}
