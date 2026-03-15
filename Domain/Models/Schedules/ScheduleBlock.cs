// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Zeitblock eines Dienstes als absolutes DateTime-Intervall.
/// Kein Mitternachts-Splitting — ein Nachtdienst bleibt ein zusammenhängender Block.
/// </summary>
/// <param name="SourceId">ID des Work/Break-Eintrags</param>
/// <param name="BlockType">Art des Blocks (Work, Break, Correction, Replacement)</param>
/// <param name="ClientId">Zugeordneter Mitarbeiter</param>
/// <param name="Start">Absoluter Startzeitpunkt</param>
/// <param name="End">Absoluter Endzeitpunkt</param>
namespace Klacks.Api.Domain.Models.Schedules;

public record ScheduleBlock(
    Guid SourceId,
    ScheduleBlockType BlockType,
    Guid ClientId,
    DateTime Start,
    DateTime End)
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
