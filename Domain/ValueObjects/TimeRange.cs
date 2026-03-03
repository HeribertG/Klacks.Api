// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.ValueObjects;

public sealed class TimeRange : IEquatable<TimeRange>
{
    public TimeOnly Start { get; }
    public TimeOnly End { get; }

    private TimeRange(TimeOnly start, TimeOnly end)
    {
        Start = start;
        End = end;
    }

    public static TimeRange Create(TimeOnly start, TimeOnly end)
    {
        return new TimeRange(start, end);
    }

    public static TimeRange CreateFromMinutes(int startMinutes, int endMinutes)
    {
        var start = TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(startMinutes));
        var end = TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(endMinutes));
        return new TimeRange(start, end);
    }

    public bool CrossesMidnight => End < Start;

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
        other is not null && Start == other.Start && End == other.End;

    public override int GetHashCode() => HashCode.Combine(Start, End);

    public static bool operator ==(TimeRange? left, TimeRange? right) =>
        left?.Equals(right) ?? right is null;

    public static bool operator !=(TimeRange? left, TimeRange? right) => !(left == right);
}
