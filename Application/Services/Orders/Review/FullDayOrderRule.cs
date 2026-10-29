// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The rules of a full-day order (StartShift equals EndShift, a 24h duty) in the order review. A time-range order
/// (IsTimeRange) is never a full-day order: its 00:00-00:00 bounds are the window a visit of WorkTime hours may fall
/// into, not a duty, so the review proposes neither a start nor a split for it. The rule further covers the equal split
/// that tiles the day from a given start, and what a start and split cover at the edges of the order period. A part whose end is
/// not after its start runs over midnight and belongs to the day it starts on, so with start 07:00 and 07-15/15-23/23-07
/// the first day stays uncovered from midnight to 07:00 and the last service runs to 07:00 of the following day; with
/// start 08:00 and 08-16/16-00/00-08 every day is covered from midnight to midnight, so the last day ends at midnight.
/// Pure; reads nothing.
/// </summary>

using Klacks.Api.Application.DTOs.Orders.Review;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant.OrderReview;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Services.Orders.Review;

public static class FullDayOrderRule
{
    private const int MinutesPerHour = 60;
    private const int MinutesPerDay = OrderReviewDefaults.HoursPerDay * MinutesPerHour;

    public static bool IsFullDay(Shift order) => order.StartShift == order.EndShift && !order.IsTimeRange;

    public static List<OrderReviewPart> EqualParts(TimeOnly start, int partCount)
    {
        var partMinutes = MinutesPerDay / partCount;
        return Enumerable.Range(0, partCount)
            .Select(index => new OrderReviewPart
            {
                Start = start.AddMinutes(index * partMinutes),
                End = start.AddMinutes((index + 1) * partMinutes),
            })
            .ToList();
    }

    public static decimal PartHours(OrderReviewPart part)
    {
        var minutes = (part.End.ToTimeSpan() - part.Start.ToTimeSpan()).TotalMinutes;
        if (minutes <= 0)
        {
            minutes += MinutesPerDay;
        }

        return (decimal)minutes / MinutesPerHour;
    }

    public static FullDayCoverage Coverage(TimeOnly start, IReadOnlyList<OrderReviewPart> parts)
    {
        IReadOnlyList<OrderReviewPart> blocks = parts.Count > 0 ? parts : [new OrderReviewPart { Start = start, End = start }];

        var firstCovered = blocks.Min(block => block.Start);
        var lastDayEnd = blocks
            .Where(block => CrossesMidnight(block) && block.End > TimeOnly.MinValue)
            .Select(block => (TimeOnly?)block.End)
            .DefaultIfEmpty(null)
            .Max();

        return new FullDayCoverage(firstCovered > TimeOnly.MinValue ? firstCovered : null, lastDayEnd);
    }

    private static bool CrossesMidnight(OrderReviewPart part) =>
        part.End <= part.Start && !(part.Start == TimeOnly.MinValue && part.End == TimeOnly.MinValue);
}
