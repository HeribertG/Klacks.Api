// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Pure validator that checks whether a set of time parts tiles an order span exactly — no gap and no
/// overlap. Times are handled on a linear timeline anchored at the order start, so cross-midnight
/// parts (e.g. 23:00-07:00) and full-day orders (start == end, treated as 24h) are covered. On a
/// violation it returns an English message naming the exact interval that is missing or overlapping.
/// </summary>

namespace Klacks.Api.Application.Common;

public static class ShiftTilingValidator
{
    private const int MinutesPerDay = 24 * 60;

    /// <summary>
    /// Returns an English error naming the offending interval, or <c>null</c> when the parts tile the
    /// order span exactly.
    /// </summary>
    /// <param name="orderStart">Start of the order span.</param>
    /// <param name="orderEnd">End of the order span (equal to start means a full 24h span).</param>
    /// <param name="parts">The candidate parts (start/end pairs) that should tile the span.</param>
    public static string? Validate(
        TimeOnly orderStart,
        TimeOnly orderEnd,
        IReadOnlyList<(TimeOnly Start, TimeOnly End)> parts)
    {
        if (parts.Count == 0)
        {
            return "No parts were supplied to tile the order span.";
        }

        var spanMinutes = SpanMinutes(orderStart, orderEnd);
        var spanText = $"{orderStart:HH\\:mm}-{orderEnd:HH\\:mm}";

        var ordered = parts
            .Select(p => new PartOffset(OffsetFromStart(orderStart, p.Start), SpanMinutes(p.Start, p.End)))
            .OrderBy(p => p.Offset)
            .ToList();

        var expectedNext = 0;
        foreach (var part in ordered)
        {
            if (part.Offset > expectedNext)
            {
                return $"The parts leave a gap between {Clock(orderStart, expectedNext)} and " +
                       $"{Clock(orderStart, part.Offset)}; the parts must tile the order span {spanText} without gaps.";
            }

            if (part.Offset < expectedNext)
            {
                return $"The parts overlap between {Clock(orderStart, part.Offset)} and " +
                       $"{Clock(orderStart, expectedNext)}; the parts must tile the order span {spanText} without overlaps.";
            }

            expectedNext = part.Offset + part.Duration;
        }

        if (expectedNext < spanMinutes)
        {
            return $"The parts leave a gap between {Clock(orderStart, expectedNext)} and " +
                   $"{orderEnd:HH\\:mm}; the parts must tile the whole order span {spanText}.";
        }

        if (expectedNext > spanMinutes)
        {
            return $"The parts overrun the order end: they extend to {Clock(orderStart, expectedNext)} but the " +
                   $"order span {spanText} ends at {orderEnd:HH\\:mm}.";
        }

        return null;
    }

    private static int SpanMinutes(TimeOnly start, TimeOnly end)
    {
        var minutes = (int)Math.Round((end.ToTimeSpan() - start.ToTimeSpan()).TotalMinutes);
        if (minutes <= 0)
        {
            minutes += MinutesPerDay;
        }

        return minutes;
    }

    private static int OffsetFromStart(TimeOnly orderStart, TimeOnly partStart)
    {
        var minutes = (int)Math.Round((partStart.ToTimeSpan() - orderStart.ToTimeSpan()).TotalMinutes) % MinutesPerDay;
        if (minutes < 0)
        {
            minutes += MinutesPerDay;
        }

        return minutes;
    }

    private static string Clock(TimeOnly orderStart, int offsetMinutes)
    {
        return orderStart.AddMinutes(offsetMinutes % MinutesPerDay).ToString("HH\\:mm");
    }

    private readonly record struct PartOffset(int Offset, int Duration);
}
