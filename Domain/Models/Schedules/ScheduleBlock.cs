// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.ScheduleOptimizer.Common.RestDays;

namespace Klacks.Api.Domain.Models.Schedules;

/// <summary>
/// Time block of a shift as an absolute DateTime interval.
/// No midnight splitting - a night shift remains a contiguous block.
/// </summary>
/// <param name="SourceId">ID of the work/break entry</param>
/// <param name="BlockType">Type of block (Work, Break, Correction, Replacement)</param>
/// <param name="ClientId">Assigned employee</param>
/// <param name="Start">Absolute start time</param>
/// <param name="End">Absolute end time</param>
/// <param name="ShiftId">Optional shift ID for travel time validation</param>
/// <param name="LocalStart">Company-local wall-clock start when <paramref name="Start"/> is UTC (DstAware); null when Start already is wall clock</param>
/// <param name="LocalEnd">Company-local wall-clock end when <paramref name="End"/> is UTC (DstAware); null when End already is wall clock</param>
public record ScheduleBlock(
    Guid SourceId,
    ScheduleBlockType BlockType,
    Guid ClientId,
    DateTime Start,
    DateTime End,
    Guid? ShiftId = null,
    DateTime? LocalStart = null,
    DateTime? LocalEnd = null)
{
    public TimeSpan Duration => End - Start;

    /// <summary>Start on the company-local calendar, the basis of the weekly rest-day count.</summary>
    public DateTime CalendarStart => LocalStart ?? Start;

    /// <summary>End on the company-local calendar, the basis of the weekly rest-day count.</summary>
    public DateTime CalendarEnd => LocalEnd ?? End;

    public DateOnly OwnerDate => DateOnly.FromDateTime(Start);

    public bool TouchesDate(DateOnly date) => CalendarWeekRestDays.Touches(Start, End, date);

    public TimeSpan GetDurationOnDate(DateOnly date)
    {
        var dayStart = date.ToDateTime(TimeOnly.MinValue);
        var dayEnd = dayStart.AddDays(1);

        var clampedStart = Start < dayStart ? dayStart : Start;
        var clampedEnd = End > dayEnd ? dayEnd : End;

        return clampedEnd > clampedStart
            ? clampedEnd - clampedStart
            : TimeSpan.Zero;
    }

    public bool Overlaps(ScheduleBlock other)
        => Start < other.End && other.Start < End;

    public TimeSpan OverlapDuration(ScheduleBlock other)
        => Overlaps(other)
            ? TimeSpan.FromTicks(Math.Min(End.Ticks, other.End.Ticks)
              - Math.Max(Start.Ticks, other.Start.Ticks))
            : TimeSpan.Zero;

    public TimeSpan GapTo(ScheduleBlock next)
        => next.Start > End ? next.Start - End : TimeSpan.Zero;
}
