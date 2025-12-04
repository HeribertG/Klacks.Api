namespace Klacks.Api.Domain.ValueObjects;

public sealed class DateRange : IEquatable<DateRange>
{
    public DateOnly From { get; }
    public DateOnly? Until { get; }

    private DateRange(DateOnly from, DateOnly? until)
    {
        From = from;
        Until = until;
    }

    public static DateRange Create(DateOnly from, DateOnly? until = null)
    {
        if (until.HasValue && until.Value < from)
            throw new ArgumentException("Until date cannot be before from date", nameof(until));

        return new DateRange(from, until);
    }

    public static DateRange CreateOpenEnded(DateOnly from)
    {
        return new DateRange(from, null);
    }

    public static DateRange CreateSingleDay(DateOnly date)
    {
        return new DateRange(date, date);
    }

    public bool IsOpenEnded => !Until.HasValue;

    public bool IsSingleDay => Until.HasValue && From == Until.Value;

    public int? TotalDays => Until.HasValue
        ? Until.Value.DayNumber - From.DayNumber + 1
        : null;

    public bool Contains(DateOnly date)
    {
        if (date < From)
            return false;

        if (Until.HasValue && date > Until.Value)
            return false;

        return true;
    }

    public bool Overlaps(DateRange other)
    {
        if (other.Until.HasValue && other.Until.Value < From)
            return false;

        if (Until.HasValue && other.From > Until.Value)
            return false;

        return true;
    }

    public IEnumerable<DateOnly> GetDates()
    {
        if (!Until.HasValue)
            throw new InvalidOperationException("Cannot enumerate dates for open-ended range");

        for (var date = From; date <= Until.Value; date = date.AddDays(1))
        {
            yield return date;
        }
    }

    public override string ToString() =>
        Until.HasValue
            ? $"{From:yyyy-MM-dd} - {Until:yyyy-MM-dd}"
            : $"{From:yyyy-MM-dd} - (open)";

    public override bool Equals(object? obj) => obj is DateRange other && Equals(other);

    public bool Equals(DateRange? other) =>
        other is not null && From == other.From && Until == other.Until;

    public override int GetHashCode() => HashCode.Combine(From, Until);

    public static bool operator ==(DateRange? left, DateRange? right) =>
        left?.Equals(right) ?? right is null;

    public static bool operator !=(DateRange? left, DateRange? right) => !(left == right);
}
