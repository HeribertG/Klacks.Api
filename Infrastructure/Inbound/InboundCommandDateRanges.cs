// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Date helpers of the inbound planning-command flow: turns the analysed weekday list into DayOfWeek values and an
/// announced period into the contiguous date ranges that match those weekdays.
/// </summary>
/// <param name="weekdays">Comma-separated ISO weekday numbers (1 = Monday ... 7 = Sunday) as the analysis reports them.</param>
/// <param name="fromDate">First day of the announced period.</param>
/// <param name="untilDate">Last day of the announced period.</param>

namespace Klacks.Api.Infrastructure.Inbound;

public static class InboundCommandDateRanges
{
    private const int FirstIsoWeekday = 1;
    private const int LastIsoWeekday = 7;
    private const int InvalidIsoWeekday = -1;
    private const char ListSeparator = ',';

    /// <summary>
    /// The whole period as one range when no weekdays are given, otherwise one range per run of consecutive matching days.
    /// </summary>
    public static List<(DateOnly From, DateOnly Until)> Build(
        DateOnly fromDate, DateOnly untilDate, HashSet<DayOfWeek> weekdays)
    {
        if (weekdays.Count == 0)
        {
            return [(fromDate, untilDate)];
        }

        var ranges = new List<(DateOnly From, DateOnly Until)>();
        var rangeDays = untilDate.DayNumber - fromDate.DayNumber + 1;
        var matching = Enumerable.Range(0, rangeDays)
            .Select(fromDate.AddDays)
            .Where(day => weekdays.Contains(day.DayOfWeek));

        foreach (var day in matching)
        {
            if (ranges.Count > 0 && ranges[^1].Until.AddDays(1) == day)
            {
                ranges[^1] = (ranges[^1].From, day);
            }
            else
            {
                ranges.Add((day, day));
            }
        }

        return ranges;
    }

    /// <summary>
    /// Every day covered by the given ranges, in order.
    /// </summary>
    public static IEnumerable<DateOnly> Days(IEnumerable<(DateOnly From, DateOnly Until)> ranges) =>
        ranges.SelectMany(range => Enumerable
            .Range(0, range.Until.DayNumber - range.From.DayNumber + 1)
            .Select(range.From.AddDays));

    /// <summary>
    /// Parses the ISO weekday list; unknown tokens are ignored.
    /// </summary>
    public static HashSet<DayOfWeek> ParseWeekdays(string? weekdays)
    {
        if (string.IsNullOrWhiteSpace(weekdays))
        {
            return [];
        }

        return weekdays
            .Split(ListSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(token => int.TryParse(token, out var isoDay) ? isoDay : InvalidIsoWeekday)
            .Where(isoDay => isoDay is >= FirstIsoWeekday and <= LastIsoWeekday)
            .Select(isoDay => isoDay == LastIsoWeekday ? DayOfWeek.Sunday : (DayOfWeek)isoDay)
            .ToHashSet();
    }
}
