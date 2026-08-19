// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// A clock-time span that knows how to measure itself across midnight, and the single place
/// where the system decides what equal bounds mean. Both conventions live here on purpose:
/// working time reads 07:00-07:00 as a 24 hour duty, absence recording reads 00:00-00:00 as
/// "no times recorded". Neither is a bug to be tidied away into the other.
/// </summary>
/// <param name="start">Start of the span</param>
/// <param name="end">End of the span; a value not greater than the start wraps past midnight
/// unless equal bounds are declared to mean an empty recording</param>
/// <param name="equalBounds">What an End equal to Start means for this span</param>
namespace Klacks.Api.Domain.ValueObjects;

using Klacks.Api.Domain.Constants;

public sealed class TimeRange : IEquatable<TimeRange>
{
    public TimeOnly Start { get; }
    public TimeOnly End { get; }
    public EqualBoundsMeaning EqualBounds { get; }

    private TimeRange(TimeOnly start, TimeOnly end, EqualBoundsMeaning equalBounds)
    {
        Start = start;
        End = end;
        EqualBounds = equalBounds;
    }

    /// <summary>
    /// A working-time span: duties, shifts and container working time. Equal bounds mean a full
    /// 24 hour day, so a 07:00-07:00 duty measures 24 hours instead of collapsing to zero.
    /// </summary>
    public static TimeRange ForWorkingTime(TimeOnly start, TimeOnly end)
    {
        return new TimeRange(start, end, EqualBoundsMeaning.FullDay);
    }

    /// <summary>
    /// An absence or break recording. Equal bounds mean no times were recorded and credit nothing,
    /// per the owner ruling of 2026-08-19. A genuine full-day absence is booked as 00:00-23:59.
    /// </summary>
    public static TimeRange ForAbsenceRecording(TimeOnly start, TimeOnly end)
    {
        return new TimeRange(start, end, EqualBoundsMeaning.NoTimeRecorded);
    }

    public static TimeRange Create(TimeOnly start, TimeOnly end, EqualBoundsMeaning equalBounds)
    {
        return new TimeRange(start, end, equalBounds);
    }

    public static TimeRange CreateFromMinutes(int startMinutes, int endMinutes, EqualBoundsMeaning equalBounds)
    {
        var start = TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(startMinutes));
        var end = TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(endMinutes));
        return new TimeRange(start, end, equalBounds);
    }

    public bool CrossesMidnight => EqualBounds == EqualBoundsMeaning.FullDay
        ? End <= Start
        : End < Start;

    public TimeSpan Duration
    {
        get
        {
            if (CrossesMidnight)
            {
                var toMidnight = TimeOnly.MaxValue.ToTimeSpan() - Start.ToTimeSpan();
                var fromMidnight = End.ToTimeSpan();
                return toMidnight + fromMidnight + TimeSpan.FromTicks(1);
            }
            return End.ToTimeSpan() - Start.ToTimeSpan();
        }
    }

    public decimal DurationInHours => (decimal)Duration.TotalHours;

    public decimal DurationInMinutes => (decimal)Duration.TotalMinutes;

    /// <summary>
    /// True when this span is the full-day booking marker 00:00-23:59. Replaces the previous
    /// "anything at or above 23.9 hours counts as a full day", which also swallowed a genuine
    /// span starting at 23:54.
    /// </summary>
    public bool IsFullDayMarker => Start == DayTimeConstants.Midnight
        && End == DayTimeConstants.LastMinuteOfDay;

    public bool Contains(TimeOnly time)
    {
        if (CrossesMidnight)
            return time >= Start || time <= End;

        return time >= Start && time <= End;
    }

    public bool Overlaps(TimeRange other)
    {
        if (CrossesMidnight || other.CrossesMidnight)
        {
            return Contains(other.Start) || Contains(other.End) ||
                   other.Contains(Start) || other.Contains(End);
        }

        return Start < other.End && End > other.Start;
    }

    public override string ToString() => $"{Start:HH:mm}-{End:HH:mm}";

    public override bool Equals(object? obj) => obj is TimeRange other && Equals(other);

    public bool Equals(TimeRange? other) =>
        other is not null && Start == other.Start && End == other.End && EqualBounds == other.EqualBounds;

    public override int GetHashCode() => HashCode.Combine(Start, End, EqualBounds);

    public static bool operator ==(TimeRange? left, TimeRange? right) =>
        left?.Equals(right) ?? right is null;

    public static bool operator !=(TimeRange? left, TimeRange? right) => !(left == right);
}
