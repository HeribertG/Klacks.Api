// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The window arithmetic of the period-cap rules: which rules are rolling-average rules, the fixed Month, Quarter,
/// Year or trailing CustomWeeks window of a date, the trailing rolling-average window clamped to the employment
/// start, and the windows the TotalHours and rolling-average rules evaluate, which are also the baseline windows read
/// ahead for several clients.
/// </summary>
/// <param name="rule">The period-cap rule the window belongs to</param>
/// <param name="date">The planned date the window is resolved for</param>
/// <param name="membershipStart">The client's employment start, null without one</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Application.Services.Schedules;

public static class PeriodCapWindows
{
    public static bool IsRollingAverageRule(PeriodCapRule rule) =>
        rule.RollingWindowWeeks.HasValue && rule.MaxAverageWeeklyHours.HasValue;

    public static (DateOnly Start, DateOnly End) ResolveFixedPeriodBoundaries(PeriodCapRule rule, DateOnly date)
    {
        switch (rule.Period)
        {
            case PeriodCapPeriod.Month:
                var monthStart = new DateOnly(date.Year, date.Month, 1);
                return (monthStart, monthStart.AddMonths(1).AddDays(-1));

            case PeriodCapPeriod.Quarter:
                var quarterStartMonth = (((date.Month - 1) / CalendarConstants.MonthsPerQuarter) * CalendarConstants.MonthsPerQuarter) + 1;
                var quarterStart = new DateOnly(date.Year, quarterStartMonth, 1);
                return (quarterStart, quarterStart.AddMonths(CalendarConstants.MonthsPerQuarter).AddDays(-1));

            case PeriodCapPeriod.Year:
                return (new DateOnly(date.Year, 1, 1), new DateOnly(date.Year, CalendarConstants.MonthsPerYear, DateTime.DaysInMonth(date.Year, CalendarConstants.MonthsPerYear)));

            case PeriodCapPeriod.CustomWeeks:
                if (!rule.CustomPeriodWeeks.HasValue || rule.CustomPeriodWeeks.Value <= 0)
                {
                    throw new NotSupportedException(
                        "PeriodCapRule with period CustomWeeks requires CustomPeriodWeeks to be set; this indicates a data integrity issue - the region-setup importer validates CustomPeriodWeeks on write.");
                }

                var windowDays = rule.CustomPeriodWeeks.Value * CalendarConstants.DaysPerWeek;
                return (date.AddDays(-(windowDays - 1)), date);

            default:
                throw new ArgumentOutOfRangeException(nameof(rule), rule.Period, "Unknown period cap period.");
        }
    }

    // The naive window is [date - RollingWindowWeeks*7 + 1, date]. Clamped to Membership.ValidFrom so a
    // recently started client is averaged only over the weeks actually worked, never over non-existent
    // pre-employment weeks (padding those with zero would artificially lower the average and hide a real
    // breach). effectiveWeeks is the number of COMPLETE weeks in the clamped window (floor division) - a
    // partial trailing week is deliberately excluded rather than counted as a fraction, which is the
    // conservative direction (it can only inflate, never hide, a violation).
    public static (DateOnly WindowStart, int EffectiveWeeks) ResolveRollingWindow(
        PeriodCapRule rule, DateOnly date, DateOnly? membershipStart)
    {
        var windowWeeks = rule.RollingWindowWeeks!.Value;
        var naiveStart = date.AddDays(-((windowWeeks * CalendarConstants.DaysPerWeek) - 1));
        var effectiveStart = membershipStart.HasValue && membershipStart.Value > naiveStart
            ? membershipStart.Value
            : naiveStart;

        if (effectiveStart > date)
        {
            return (effectiveStart, 0);
        }

        var effectiveDays = date.DayNumber - effectiveStart.DayNumber + 1;
        return (effectiveStart, effectiveDays / CalendarConstants.DaysPerWeek);
    }

    /// <summary>
    /// The windows a fixed-period rule evaluates: the planned (date, hours) pairs grouped by the window of their date,
    /// in the order the windows first occur.
    /// </summary>
    /// <param name="rule">The fixed-period rule</param>
    /// <param name="plannedHours">The client's not-yet-saved (date, hours) pairs</param>
    public static IEnumerable<IGrouping<(DateOnly Start, DateOnly End), (DateOnly Date, decimal Hours)>> FixedPeriodWindows(
        PeriodCapRule rule,
        IReadOnlyList<(DateOnly Date, decimal Hours)> plannedHours)
        => plannedHours.GroupBy(p => ResolveFixedPeriodBoundaries(rule, p.Date));

    /// <summary>
    /// The windows a rolling-average rule evaluates: per distinct planned date in ascending order the clamped trailing
    /// window ending on it. A date whose window holds no complete week yet is left out: averaging over less than a week
    /// would divide by (near) zero or, padded with zero-hour weeks, hide a real breach.
    /// </summary>
    /// <param name="rule">The rolling-average rule</param>
    /// <param name="plannedHours">The client's not-yet-saved (date, hours) pairs</param>
    /// <param name="membershipStart">The client's employment start, null without one</param>
    public static IEnumerable<(DateOnly Date, DateOnly WindowStart, int EffectiveWeeks)> RollingAverageWindows(
        PeriodCapRule rule,
        IReadOnlyList<(DateOnly Date, decimal Hours)> plannedHours,
        DateOnly? membershipStart)
    {
        foreach (var date in plannedHours.Select(p => p.Date).Distinct().OrderBy(d => d))
        {
            var (windowStart, effectiveWeeks) = ResolveRollingWindow(rule, date, membershipStart);
            if (effectiveWeeks > 0)
            {
                yield return (date, windowStart, effectiveWeeks);
            }
        }
    }

    /// <summary>
    /// The baseline windows the TotalHours and rolling-average rules read for one client: the windows of
    /// <see cref="FixedPeriodWindows"/> and <see cref="RollingAverageWindows"/>, the same ones the evaluation reads.
    /// </summary>
    /// <param name="plannedHours">The client's not-yet-saved (date, hours) pairs</param>
    /// <param name="rules">The rules that apply to the client</param>
    /// <param name="membershipStart">The client's employment start, null without one</param>
    public static IEnumerable<(DateOnly Start, DateOnly End)> BaselineWindows(
        IReadOnlyList<(DateOnly Date, decimal Hours)> plannedHours,
        IReadOnlyList<PeriodCapRule> rules,
        DateOnly? membershipStart)
    {
        foreach (var rule in rules)
        {
            if (IsRollingAverageRule(rule))
            {
                foreach (var window in RollingAverageWindows(rule, plannedHours, membershipStart))
                {
                    yield return (window.WindowStart, window.Date);
                }
            }
            else if (rule.Scope == PeriodCapScope.TotalHours)
            {
                foreach (var window in FixedPeriodWindows(rule, plannedHours))
                {
                    yield return window.Key;
                }
            }
        }
    }
}
