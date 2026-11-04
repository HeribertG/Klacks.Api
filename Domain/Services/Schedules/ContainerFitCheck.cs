// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Owner rule "a container holds all of its services": if a service, an absence or a part of a service (travel, briefing,
/// debriefing) reaches outside the container, the container cannot be saved. One check for the container in the schedule
/// (sub-works, sub-breaks and the time-carrying work changes of the sub-works) and for the container template (items with
/// their travel, briefing and debriefing durations, likewise the day override of a container). Work changes without a
/// span (equal start and end, as replacements store them) carry no time window and are not checked. Overlap rules are
/// those of ContainerChildrenEnvelopeValidator.
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
    public static void EnsureTemplateFits(ContainerTemplate template)
        => EnsureFits(
            template.FromTime,
            template.UntilTime,
            ItemSpans(template.ContainerTemplateItems.Select(i => (i.StartItem, i.EndItem, i.BriefingTime, i.TravelTimeBefore, i.DebriefingTime, i.TravelTimeAfter))));

    /// <summary>Throws ContainerFitRefusedException when an item of the day override, with its durations around it, reaches outside of the override.</summary>
    /// <param name="containerOverride">The day-specific override of a container with its items</param>
    public static void EnsureOverrideFits(ContainerShiftOverride containerOverride)
        => EnsureFits(
            containerOverride.FromTime,
            containerOverride.UntilTime,
            ItemSpans(containerOverride.ContainerShiftOverrideItems.Select(i => (i.StartItem, i.EndItem, i.BriefingTime, i.TravelTimeBefore, i.DebriefingTime, i.TravelTimeAfter))));

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
