// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decides from a person's payroll export ranges whether a pay period lies behind the recalculation window: an export
/// range overlaps the period and the period ends before the earliest start of the person's latest
/// PayPeriodLimits.RecalculableExportRanges distinct export ranges (latest by end date, then by start date). Supplementary
/// exports and revisions of the same range count once. A person with at most that many distinct ranges has no period
/// behind the window. Whether the period is sealed is decided elsewhere (IFrozenPeriodPolicy).
/// </summary>
/// <param name="exportRanges">All export ranges of one person, duplicates allowed</param>
/// <param name="periodStart">First day of the pay period</param>
/// <param name="periodEnd">Last day of the pay period</param>

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Schedules;

public static class ExportedPeriodWindow
{
    public static bool IsBehindWindow(
        IEnumerable<(DateOnly StartDate, DateOnly EndDate)> exportRanges,
        DateOnly periodStart,
        DateOnly periodEnd)
    {
        var distinctRanges = exportRanges.Distinct().ToList();
        if (distinctRanges.Count <= PayPeriodLimits.RecalculableExportRanges)
        {
            return false;
        }

        var windowStart = distinctRanges
            .OrderByDescending(range => range.EndDate)
            .ThenByDescending(range => range.StartDate)
            .Take(PayPeriodLimits.RecalculableExportRanges)
            .Min(range => range.StartDate);

        var exported = distinctRanges.Any(range => range.StartDate <= periodEnd && range.EndDate >= periodStart);
        return exported && periodEnd < windowStart;
    }
}
