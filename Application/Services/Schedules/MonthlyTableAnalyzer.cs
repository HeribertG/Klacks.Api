// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The one analysis of the company-wide monthly target hours table both setup advisors (absence hours, target hours)
/// report: which months of a year lack a row, and which month values deviate from workdays x daily hours by more than
/// the named threshold, so a full month of vacation would not reach the month's target.
/// </summary>
/// <param name="rows">Monthly rows of the year, month -> hours</param>
/// <param name="workdays">Workdays of every month of the year</param>
/// <param name="dailyHours">Effective daily working hours</param>

using System.Globalization;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.Services.Schedules;

public static class MonthlyTableAnalyzer
{
    // 10 % absorbs rounding and a single bridging day; anything beyond is a value that does not fit the month.
    public const decimal DeviationThresholdPercent = 10m;

    private const int Decimals = 2;

    public static IReadOnlyList<int> MissingMonths(IReadOnlyDictionary<int, decimal> rows) =>
        rows.Count == 0
            ? []
            : Enumerable.Range(MonthlyTargetHoursConstants.MinimumMonth, MonthlyTargetHoursConstants.MaximumMonth)
                .Where(month => !rows.ContainsKey(month))
                .ToList();

    public static bool IsPartiallyFilled(IReadOnlyDictionary<int, decimal> rows) =>
        rows.Count > 0 && rows.Count < MonthlyTargetHoursConstants.MaximumMonth;

    /// <summary>Month values beyond the threshold, described as "month: hours vs workdays x daily = expected (deviation %)".</summary>
    public static IReadOnlyList<string> DeviatingMonths(
        IReadOnlyDictionary<int, decimal> rows, IReadOnlyDictionary<int, int> workdays, decimal dailyHours)
    {
        if (dailyHours <= decimal.Zero)
        {
            return [];
        }

        var deviating = new List<string>();
        foreach (var (month, hours) in rows.OrderBy(entry => entry.Key))
        {
            if (!workdays.TryGetValue(month, out var days) || days == 0)
            {
                continue;
            }

            var expected = days * dailyHours;
            var deviationPercent = Math.Abs(hours - expected) / expected * MonthlyTargetHoursConstants.FullWorkloadPercent;
            if (deviationPercent > DeviationThresholdPercent)
            {
                deviating.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}: {1} h vs {2} workdays x {3} h = {4} h ({5} %)",
                    month, Round(hours), days, dailyHours, Round(expected), Round(deviationPercent)));
            }
        }

        return deviating;
    }

    private static decimal Round(decimal value) => Math.Round(value, Decimals, MidpointRounding.AwayFromZero);
}
