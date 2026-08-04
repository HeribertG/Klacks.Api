// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Evaluates whether an additional absence request still leaves enough staffing reserve.
/// Capacity is the desired daily readiness; from it the already planned absences and the
/// requested one are subtracted. A window fails when demand divided by what remains exceeds
/// the configured utilization ceiling, which is what keeps a buffer for unplanned sickness.
/// Windows are evaluated per day, per rolling three days, per work week and per calendar week,
/// because a shortage that is harmless on a single day becomes critical when it repeats.
/// Ratios are computed from window sums, never from averaged per-day ratios, so a day with
/// many shifts weighs more than a quiet one.
/// </summary>

namespace Klacks.Api.Domain.Services.Schedules;

public static class AbsenceCapacityCalculator
{
    private const double CapacityEpsilon = 0.0001;
    private const int ThreeDayWindowLength = 3;
    private const int DaysPerWeek = 7;

    /// <summary>
    /// Evaluates every window that touches the requested period.
    /// </summary>
    /// <param name="days">Per-day readiness, demand and already planned absence, covering at least the request plus one week on each side</param>
    /// <param name="requestFrom">First day of the requested absence</param>
    /// <param name="requestUntil">Last day of the requested absence</param>
    /// <param name="requestedDailyValue">Absence value the request adds on each of its calendar days, in the same unit as the existing absences</param>
    public static IReadOnlyList<CapacityWindowFinding> Evaluate(
        IReadOnlyList<CapacityDay> days,
        DateOnly requestFrom,
        DateOnly requestUntil,
        double requestedDailyValue)
    {
        ArgumentNullException.ThrowIfNull(days);

        if (requestUntil < requestFrom)
        {
            (requestFrom, requestUntil) = (requestUntil, requestFrom);
        }

        var byDate = days
            .GroupBy(d => d.Date)
            .ToDictionary(g => g.Key, g => g.First());

        var ordered = byDate.Values.OrderBy(d => d.Date).ToList();
        if (ordered.Count == 0)
        {
            return [];
        }

        var findings = new List<CapacityWindowFinding>();

        foreach (var day in ordered)
        {
            if (day.Date < requestFrom || day.Date > requestUntil)
            {
                continue;
            }

            var finding = BuildFinding(CapacityWindowKind.Day, [day], requestFrom, requestUntil, requestedDailyValue);
            if (finding != null)
            {
                findings.Add(finding);
            }
        }

        for (var i = 0; i + ThreeDayWindowLength - 1 < ordered.Count; i++)
        {
            var window = ordered.GetRange(i, ThreeDayWindowLength);
            if (!TouchesRequest(window, requestFrom, requestUntil))
            {
                continue;
            }

            var finding = BuildFinding(CapacityWindowKind.ThreeDay, window, requestFrom, requestUntil, requestedDailyValue);
            if (finding != null)
            {
                findings.Add(finding);
            }
        }

        foreach (var week in GroupByCalendarWeek(ordered))
        {
            if (!TouchesRequest(week, requestFrom, requestUntil))
            {
                continue;
            }

            var calendarWeek = BuildFinding(CapacityWindowKind.CalendarWeek, week, requestFrom, requestUntil, requestedDailyValue);
            if (calendarWeek != null)
            {
                findings.Add(calendarWeek);
            }

            var workDays = week.Where(d => d.DesiredReadiness > CapacityEpsilon).ToList();
            if (workDays.Count == 0 || workDays.Count == week.Count)
            {
                continue;
            }

            var workWeek = BuildFinding(CapacityWindowKind.WorkWeek, workDays, requestFrom, requestUntil, requestedDailyValue);
            if (workWeek != null)
            {
                findings.Add(workWeek);
            }
        }

        return findings;
    }

    /// <summary>
    /// Filters an evaluation down to the windows that break the ceiling or have no capacity left.
    /// </summary>
    public static IReadOnlyList<CapacityWindowFinding> CriticalOnly(
        IEnumerable<CapacityWindowFinding> findings,
        double maxUtilization)
    {
        return findings
            .Where(f => f.NoCapacityLeft || (f.Utilization.HasValue && f.Utilization.Value > maxUtilization))
            .ToList();
    }

    /// <summary>
    /// Slides a period of the requested length across the search range and judges each placement with
    /// the very same rule Evaluate applies, so a candidate reported as fitting here can never be
    /// rejected by a direct check of the same period.
    /// </summary>
    /// <param name="days">Per-day readiness, demand and already planned absence covering the search range plus one week on each side</param>
    /// <param name="searchFrom">Earliest day the period may start on</param>
    /// <param name="searchUntil">Latest day the period may end on</param>
    /// <param name="durationDays">Length of the wanted period in calendar days</param>
    /// <param name="requestedDailyValue">Absence value the request would add on each of its calendar days</param>
    /// <param name="maxUtilization">Highest tolerated demand-to-available ratio</param>
    public static IReadOnlyList<CapacityWindowCandidate> FindFittingPeriods(
        IReadOnlyList<CapacityDay> days,
        DateOnly searchFrom,
        DateOnly searchUntil,
        int durationDays,
        double requestedDailyValue,
        double maxUtilization)
    {
        ArgumentNullException.ThrowIfNull(days);

        if (durationDays < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(durationDays), durationDays, "Duration must be at least one day.");
        }

        if (searchUntil < searchFrom)
        {
            (searchFrom, searchUntil) = (searchUntil, searchFrom);
        }

        var candidates = new List<CapacityWindowCandidate>();
        var lastStart = searchUntil.AddDays(-(durationDays - 1));

        for (var start = searchFrom; start <= lastStart; start = start.AddDays(1))
        {
            var end = start.AddDays(durationDays - 1);
            var findings = Evaluate(days, start, end, requestedDailyValue);
            if (findings.Count == 0)
            {
                continue;
            }

            var blocking = CriticalOnly(findings, maxUtilization);
            var peak = findings.Any(f => f.NoCapacityLeft)
                ? (double?)null
                : findings.Where(f => f.Utilization.HasValue).Max(f => f.Utilization!.Value);

            candidates.Add(new CapacityWindowCandidate(start, end, blocking.Count == 0, peak, blocking.Count));
        }

        return candidates;
    }

    private static bool TouchesRequest(IReadOnlyList<CapacityDay> window, DateOnly requestFrom, DateOnly requestUntil)
    {
        return window.Any(d => d.Date >= requestFrom && d.Date <= requestUntil);
    }

    private static List<List<CapacityDay>> GroupByCalendarWeek(List<CapacityDay> ordered)
    {
        return ordered
            .GroupBy(d => d.Date.AddDays(-DaysSinceMonday(d.Date)))
            .OrderBy(g => g.Key)
            .Select(g => g.OrderBy(d => d.Date).ToList())
            .ToList();
    }

    private static int DaysSinceMonday(DateOnly date)
    {
        return ((int)date.DayOfWeek + (DaysPerWeek - 1)) % DaysPerWeek;
    }

    private static CapacityWindowFinding? BuildFinding(
        CapacityWindowKind kind,
        IReadOnlyList<CapacityDay> window,
        DateOnly requestFrom,
        DateOnly requestUntil,
        double requestedDailyValue)
    {
        double demand = 0;
        double available = 0;

        foreach (var day in window)
        {
            var requested = day.Date >= requestFrom && day.Date <= requestUntil ? requestedDailyValue : 0;
            demand += day.Demand;
            available += day.DesiredReadiness - day.ExistingAbsence - requested;
        }

        if (available <= CapacityEpsilon)
        {
            return demand <= CapacityEpsilon
                ? null
                : new CapacityWindowFinding(kind, window[0].Date, window[^1].Date, demand, available, null, true);
        }

        return new CapacityWindowFinding(kind, window[0].Date, window[^1].Date, demand, available, demand / available, false);
    }
}
