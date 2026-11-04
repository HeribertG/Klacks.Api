// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decides which children of a container go to the second part of a split. A container is split only between two of its
/// children, never through one, and both parts keep at least one task. Times are compared relative to the container start,
/// so a container crossing midnight works; a container with equal start and end covers the whole day.
/// </summary>
/// <param name="containerStart">Start of the container</param>
/// <param name="containerEnd">End of the container</param>
/// <param name="splitTime">Time the second part starts</param>
/// <param name="items">The live sub-works and sub-breaks of the container</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Schedules;

public static class ContainerSplitPartition
{
    private const int MinutesPerHour = 60;
    private const int MinutesPerDay = 24 * MinutesPerHour;
    private const int NextDay = 1;

    /// <summary>Returns the ids of the children that move to the second part; throws InvalidRequestException when the split is not allowed.</summary>
    public static IReadOnlySet<Guid> ItemsAfter(
        TimeOnly containerStart, TimeOnly containerEnd, TimeOnly splitTime, IReadOnlyCollection<ContainerSplitItem> items)
    {
        var origin = ToMinutes(containerStart);
        var length = Span(origin, ToMinutes(containerEnd));
        if (length == 0)
        {
            length = MinutesPerDay;
        }

        var split = Span(origin, ToMinutes(splitTime));
        if (split <= 0 || split >= length)
        {
            throw new InvalidRequestException(ContainerSplitMessages.OutsideContainer);
        }

        var after = new HashSet<Guid>();
        var tasksBefore = 0;
        var tasksAfter = 0;
        foreach (var item in items)
        {
            var start = Span(origin, ToMinutes(item.Start));
            var end = start + Span(ToMinutes(item.Start), ToMinutes(item.End));
            if (end <= split)
            {
                tasksBefore += item.IsTask ? 1 : 0;
            }
            else if (start >= split)
            {
                tasksAfter += item.IsTask ? 1 : 0;
                after.Add(item.Id);
            }
            else
            {
                throw new InvalidRequestException(ContainerSplitMessages.CutsThroughItem);
            }
        }

        if (tasksBefore == 0 || tasksAfter == 0)
        {
            throw new InvalidRequestException(ContainerSplitMessages.BothPartsNeedATask);
        }

        return after;
    }

    /// <summary>
    /// The span a service occupies in the container: the service itself widened by its time-carrying work changes (travel,
    /// briefing, debriefing), measured from the container start. Work changes without a span are ignored.
    /// </summary>
    public static (TimeOnly Start, TimeOnly End) Occupied(
        TimeOnly containerStart, TimeOnly start, TimeOnly end, IEnumerable<(TimeOnly Start, TimeOnly End)> parts)
    {
        var origin = ToMinutes(containerStart);
        var spans = new List<(TimeOnly Start, TimeOnly End)>(parts.Where(p => p.Start != p.End)) { (start, end) };
        var first = spans.MinBy(p => Span(origin, ToMinutes(p.Start)));
        var last = spans.MaxBy(p => Span(origin, ToMinutes(p.Start)) + Span(ToMinutes(p.Start), ToMinutes(p.End)));
        return (first.Start, last.End);
    }

    /// <summary>The day of the second part: the next day when the split time lies after midnight of the container day.</summary>
    public static DateOnly SecondPartDate(DateOnly containerDate, TimeOnly containerStart, TimeOnly splitTime)
        => splitTime < containerStart ? containerDate.AddDays(NextDay) : containerDate;

    private static int ToMinutes(TimeOnly time) => time.Hour * MinutesPerHour + time.Minute;

    private static int Span(int from, int to) => (to - from + MinutesPerDay) % MinutesPerDay;
}
