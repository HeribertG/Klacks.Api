// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
namespace Klacks.Api.Domain.Models.Schedules;

public record ScheduleBlock(
    Guid SourceId,
    ScheduleBlockType BlockType,
    Guid ClientId,
    DateTime Start,
    DateTime End,
    Guid? ShiftId = null)
{
    public TimeSpan Duration => End - Start;

    public DateOnly OwnerDate => DateOnly.FromDateTime(Start);

    public bool TouchesDate(DateOnly date)
    {
        var dayStart = date.ToDateTime(TimeOnly.MinValue);
        var dayEnd = dayStart.AddDays(1);
        return Start < dayEnd && End > dayStart;
    }

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
