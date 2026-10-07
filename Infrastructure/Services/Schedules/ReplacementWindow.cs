// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Effective time window of a replacement WorkChange. ReplacementStart and ReplacementEnd are stored without own times
/// (the UI sends 00:00/00:00): the substitute takes the first resp. last ChangeTime hours of the work. Only
/// ReplacementWithin carries its own span. Mirrors the get_schedule_entries stored procedure; single source for the
/// schedule display service and the planning wizards.
/// </summary>
/// <param name="type">Replacement type of the WorkChange</param>
/// <param name="workStart">Start time of the work</param>
/// <param name="workEnd">End time of the work</param>
/// <param name="changeStart">Stored start of the WorkChange (used for ReplacementWithin)</param>
/// <param name="changeEnd">Stored end of the WorkChange (used for ReplacementWithin)</param>
/// <param name="changeTime">Hours handed to the substitute</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public static class ReplacementWindow
{
    private const decimal MillisecondsPerHour = 3_600_000m;
    private const decimal HoursPerDay = 24m;

    public static (TimeOnly Start, TimeOnly End) Compute(
        WorkChangeType type, TimeOnly workStart, TimeOnly workEnd, TimeOnly changeStart, TimeOnly changeEnd, decimal changeTime) =>
        type switch
        {
            WorkChangeType.ReplacementStart => (workStart, AddHours(workStart, changeTime)),
            WorkChangeType.ReplacementEnd => (AddHours(workEnd, -changeTime), workEnd),
            _ => (changeStart, changeEnd),
        };

    /// <summary>
    /// Places a window of a work on the calendar. A window starting before the work's start lies after midnight of a
    /// night shift and therefore on the next day; an end at or before the start ends on the day after the start.
    /// </summary>
    public static (DateTime StartAt, DateTime EndAt) ToInterval(DateOnly workDate, TimeOnly workStart, TimeOnly start, TimeOnly end)
    {
        var startDate = start < workStart ? workDate.AddDays(1) : workDate;
        var startAt = startDate.ToDateTime(start);
        var endAt = end <= start ? startDate.AddDays(1).ToDateTime(end) : startDate.ToDateTime(end);
        return (startAt, endAt);
    }

    /// <summary>
    /// Hours handed over, limited to the length of the work: a ChangeTime longer than the work would otherwise wrap
    /// the window past the work's start and block the substitute for more than the shift. Zero or less means the
    /// replacement hands nothing over and is no occupancy at all.
    /// </summary>
    public static decimal ClampHours(TimeOnly workStart, TimeOnly workEnd, decimal changeTime) =>
        Math.Max(0m, Math.Min(changeTime, WorkLengthHours(workStart, workEnd)));

    public static decimal WorkLengthHours(TimeOnly workStart, TimeOnly workEnd)
    {
        var hours = (decimal)(workEnd - workStart).TotalHours;
        return hours <= 0m ? hours + HoursPerDay : hours;
    }

    internal static TimeOnly AddHours(TimeOnly time, decimal hours)
    {
        var totalMs = (long)decimal.Round(hours * MillisecondsPerHour);
        var span = time.ToTimeSpan().Add(TimeSpan.FromMilliseconds(totalMs));
        var normalized = TimeSpan.FromTicks(((span.Ticks % TimeSpan.TicksPerDay) + TimeSpan.TicksPerDay) % TimeSpan.TicksPerDay);
        return TimeOnly.FromTimeSpan(normalized);
    }
}
