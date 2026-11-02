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

namespace Klacks.Api.Domain.Services.Schedules;

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
    /// The calendar interval a replacement really occupies. False when it hands nothing over. ReplacementStart and
    /// ReplacementEnd hand over <see cref="ClampHours"/> hours, so none left means nothing; a full-length work of 24 hours
    /// gives equal bounds there, which <see cref="ToInterval"/> reads as the full day. Every other type carries its own
    /// span, so equal bounds mean nothing was entered and its ChangeTime is not consulted: the repository derives it from
    /// the span only when the change is stored, so a caller checking a change before that sees a raw value.
    /// </summary>
    /// <param name="type">Replacement type of the WorkChange</param>
    /// <param name="workStart">Start time of the work</param>
    /// <param name="workEnd">End time of the work</param>
    /// <param name="workDate">Date of the work</param>
    /// <param name="changeStart">Stored start of the WorkChange (used for ReplacementWithin)</param>
    /// <param name="changeEnd">Stored end of the WorkChange (used for ReplacementWithin)</param>
    /// <param name="changeTime">Hours handed to the substitute</param>
    /// <param name="startAt">Start of the occupied interval</param>
    /// <param name="endAt">End of the occupied interval</param>
    public static bool TryGetInterval(
        WorkChangeType type,
        TimeOnly workStart,
        TimeOnly workEnd,
        DateOnly workDate,
        TimeOnly changeStart,
        TimeOnly changeEnd,
        decimal changeTime,
        out DateTime startAt,
        out DateTime endAt)
    {
        startAt = default;
        endAt = default;

        var hours = ClampHours(workStart, workEnd, changeTime);
        var (start, end) = Compute(type, workStart, workEnd, changeStart, changeEnd, hours);
        var handsNothingOver = type is WorkChangeType.ReplacementStart or WorkChangeType.ReplacementEnd
            ? hours <= 0m
            : start == end;
        if (handsNothingOver)
        {
            return false;
        }

        (startAt, endAt) = ToInterval(workDate, workStart, start, end);
        return true;
    }

    /// <summary>
    /// Hours a ReplacementStart or ReplacementEnd hands over, limited to the length of the work: a ChangeTime longer than
    /// the work would otherwise wrap the window past the work's start and block the substitute for more than the shift.
    /// Zero or less means such a replacement hands nothing over and is no occupancy at all; a ReplacementWithin is
    /// decided by its own span and does not use this value.
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
