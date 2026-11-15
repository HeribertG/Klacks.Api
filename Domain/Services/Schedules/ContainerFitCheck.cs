// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Owner rule "a container holds all of its services": if a service, an absence or a part of a service (travel, briefing,
/// debriefing) reaches outside the container, the container cannot be saved. One check for the container in the schedule
/// (sub-works, sub-breaks and the time-carrying work changes of the sub-works) and for the container template (items with
/// their travel, briefing and debriefing durations, likewise the day override of a container). A template or override item
/// is checked with the time it occupies once expanded (as ContainerWorkExpansionService builds the child work): the planned
/// visit time (TimeRangeStartItem/TimeRangeEndItem) of an item whose shift is a TimeRange shift, the own time
/// (StartItem/EndItem) of every other item; an order window wider than the container is no violation by itself. Work
/// changes without a span (equal start and end, as replacements store them) carry no time window and are not checked.
/// Overlap rules are those of ContainerChildrenEnvelopeValidator.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Schedules;

public static class ContainerFitCheck
{
    private const string ListSeparator = ", ";

    /// <summary>Throws ContainerFitRefusedException when a child of the container reaches outside of it.</summary>
    /// <param name="containerStart">Start of the container as it will be saved</param>
    /// <param name="containerEnd">End of the container as it will be saved</param>
    public static void EnsureChildrenFit(
        TimeOnly containerStart,
        TimeOnly containerEnd,
        IEnumerable<Work> works,
        IEnumerable<Break> breaks,
        IEnumerable<WorkChange> workChanges)
    {
        var items = works.Select(w => (w.StartTime, w.EndTime))
            .Concat(breaks.Select(b => (b.StartTime, b.EndTime)))
            .Concat(workChanges.Where(wc => wc.StartTime != wc.EndTime).Select(wc => (wc.StartTime, wc.EndTime)));
        EnsureFits(containerStart, containerEnd, items);
    }

    /// <summary>Throws ContainerFitRefusedException when an item of the template, with its durations around it, reaches outside of the template.</summary>
    /// <param name="template">The template with its items</param>
    /// <param name="timeRangeShiftIds">Ids of the item shifts that are TimeRange shifts (their planned visit time is the occupied time)</param>
    public static void EnsureTemplateFits(ContainerTemplate template, IReadOnlySet<Guid> timeRangeShiftIds)
        => EnsureFits(
            template.FromTime,
            template.UntilTime,
            ItemSpans(template.ContainerTemplateItems.Select(i => (
                OccupiedStart(i.ShiftId, i.StartItem, i.TimeRangeStartItem, timeRangeShiftIds),
                OccupiedEnd(i.ShiftId, i.EndItem, i.TimeRangeEndItem, timeRangeShiftIds),
                i.BriefingTime, i.TravelTimeBefore, i.DebriefingTime, i.TravelTimeAfter))));

    /// <summary>Throws ContainerFitRefusedException when an item of the day override, with its durations around it, reaches outside of the override.</summary>
    /// <param name="containerOverride">The day-specific override of a container with its items</param>
    /// <param name="timeRangeShiftIds">Ids of the item shifts that are TimeRange shifts (their planned visit time is the occupied time)</param>
    public static void EnsureOverrideFits(ContainerShiftOverride containerOverride, IReadOnlySet<Guid> timeRangeShiftIds)
        => EnsureFits(
            containerOverride.FromTime,
            containerOverride.UntilTime,
            ItemSpans(containerOverride.ContainerShiftOverrideItems.Select(i => (
                OccupiedStart(i.ShiftId, i.StartItem, i.TimeRangeStartItem, timeRangeShiftIds),
                OccupiedEnd(i.ShiftId, i.EndItem, i.TimeRangeEndItem, timeRangeShiftIds),
                i.BriefingTime, i.TravelTimeBefore, i.DebriefingTime, i.TravelTimeAfter))));

    private static TimeOnly? OccupiedStart(Guid? shiftId, TimeOnly? startItem, TimeOnly? timeRangeStartItem, IReadOnlySet<Guid> timeRangeShiftIds)
        => UsesPlannedTime(shiftId, timeRangeShiftIds) && timeRangeStartItem.HasValue ? timeRangeStartItem : startItem;

    private static TimeOnly? OccupiedEnd(Guid? shiftId, TimeOnly? endItem, TimeOnly? timeRangeEndItem, IReadOnlySet<Guid> timeRangeShiftIds)
        => UsesPlannedTime(shiftId, timeRangeShiftIds) && timeRangeEndItem.HasValue ? timeRangeEndItem : endItem;

    private static bool UsesPlannedTime(Guid? shiftId, IReadOnlySet<Guid> timeRangeShiftIds)
        => shiftId.HasValue && timeRangeShiftIds.Contains(shiftId.Value);

    private static IEnumerable<(TimeOnly Start, TimeOnly End)> ItemSpans(
        IEnumerable<(TimeOnly? Start, TimeOnly? End, TimeOnly Briefing, TimeOnly TravelBefore, TimeOnly Debriefing, TimeOnly TravelAfter)> items)
        => items
            .Where(item => item.Start.HasValue && item.End.HasValue)
            .Select(item => (
                item.Start!.Value.Add(-(item.Briefing.ToTimeSpan() + item.TravelBefore.ToTimeSpan())),
                item.End!.Value.Add(item.Debriefing.ToTimeSpan() + item.TravelAfter.ToTimeSpan())));

    private static void EnsureFits(TimeOnly start, TimeOnly end, IEnumerable<(TimeOnly Start, TimeOnly End)> items)
    {
        var outside = ContainerChildrenEnvelopeValidator.FindOverhangs(start, end, items);
        if (outside.Count == 0)
        {
            return;
        }

        throw new ContainerFitRefusedException(
            Format(start, end), string.Join(ListSeparator, outside.Select(o => Format(o.Start, o.End))));
    }

    private static string Format(TimeOnly start, TimeOnly end)
        => $"{start.ToString(DateTimeFormats.HourMinute, CultureInfo.InvariantCulture)}-{end.ToString(DateTimeFormats.HourMinute, CultureInfo.InvariantCulture)}";
}
