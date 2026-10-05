// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The calendar days a work actually covers, the single rule every holiday-work caller uses: a night shift
/// 31.07 22:00-06:00 covers 31.07 and 01.08, so work on the 01.08 holiday is found although the work belongs
/// to 31.07. The interval is end exclusive (a work ending at 00:00 does not cover the next day) and is read on
/// the company-local calendar (ScheduleBlock.CalendarStart/CalendarEnd), never on the UTC instants of a
/// DST-aware timeline. Work, correction and replacement blocks count as work; breaks (absences) never do.
/// Planned rows wrap like TimelineCalculationService.CreateBlock: an end at or before the start continues on
/// the next day, so a row with start == end is a 24-hour work.
/// </summary>

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Schedules;

public static class WorkedCalendarDates
{
    private static readonly HashSet<ScheduleBlockType> WorkingBlockTypes =
    [
        ScheduleBlockType.Work,
        ScheduleBlockType.Correction,
        ScheduleBlockType.Replacement
    ];

    /// <summary>
    /// Days covered by a planned work row on <paramref name="date"/> from <paramref name="start"/> to <paramref name="end"/>.
    /// </summary>
    public static List<DateOnly> FromTimeRange(DateOnly date, TimeOnly start, TimeOnly end)
    {
        var startAt = date.ToDateTime(start);
        var endAt = end <= start ? date.AddDays(1).ToDateTime(end) : date.ToDateTime(end);
        return DatesBetween(startAt, endAt).ToList();
    }

    /// <summary>
    /// Distinct, ordered days covered by the working blocks among <paramref name="blocks"/>.
    /// </summary>
    public static List<DateOnly> FromBlocks(IEnumerable<ScheduleBlock> blocks)
    {
        return blocks
            .Where(IsWork)
            .SelectMany(block => DatesBetween(block.CalendarStart, block.CalendarEnd))
            .Distinct()
            .Order()
            .ToList();
    }

    /// <summary>
    /// True when the block is time the client actually works (not an absence).
    /// </summary>
    public static bool IsWork(ScheduleBlock block) => WorkingBlockTypes.Contains(block.BlockType);

    private static IEnumerable<DateOnly> DatesBetween(DateTime start, DateTime end)
    {
        var first = DateOnly.FromDateTime(start);
        var last = end > start ? DateOnly.FromDateTime(end.AddTicks(-1)) : first;

        for (var day = first; day <= last; day = day.AddDays(1))
        {
            yield return day;
        }
    }
}
