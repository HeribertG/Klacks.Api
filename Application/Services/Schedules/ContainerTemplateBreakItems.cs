// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The break item of a container weekday template in the shape the container page stores it, the place a fixed break is
/// inserted, the equality add_container_template_break verifies by, and the end of the last visit of a template. With a
/// start time the break is fixed (StartItem/EndItem); without it the break is movable (no StartItem/EndItem, its duration
/// as the TimeRange 00:00..00:00+duration) and only reserves its duration when the route is optimized.
/// </summary>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Services.RouteOptimization.VisitScheduling;

namespace Klacks.Api.Application.Services.Schedules;

public static class ContainerTemplateBreakItems
{
    /// <param name="absenceId">Absence type the break is booked with</param>
    /// <param name="startTime">Start of a fixed break; null for a movable break</param>
    /// <param name="durationMinutes">Length of the break in minutes</param>
    public static ContainerTemplateItemResource Build(Guid absenceId, TimeOnly? startTime, int durationMinutes)
    {
        var item = new ContainerTemplateItemResource
        {
            Id = Guid.Empty,
            ShiftId = null,
            AbsenceId = absenceId,
            TransportMode = TransportMode.ByCar
        };

        if (startTime.HasValue)
        {
            item.StartItem = startTime.Value;
            item.EndItem = startTime.Value.AddMinutes(durationMinutes);
            return item;
        }

        var movableStart = ContainerVisitScheduler.ClockTime(ContainerVisitDefaults.MovableBlockDefaultStartMinutes);
        item.TimeRangeStartItem = movableStart;
        item.TimeRangeEndItem = movableStart.AddMinutes(durationMinutes);
        return item;
    }

    /// <param name="items">Items of the template in route order</param>
    /// <param name="breakItem">The break to add: a fixed break goes before the first item that starts later, a movable one is appended</param>
    public static List<ContainerTemplateItemResource> Insert(
        IReadOnlyList<ContainerTemplateItemResource> items, ContainerTemplateItemResource breakItem)
    {
        return breakItem.StartItem.HasValue
            ? ContainerTemplateItemOrder.Interleave<ContainerTemplateItemResource>([breakItem], items, ContainerTemplateItemStart.Minutes)
            : [.. items, breakItem];
    }

    /// <param name="items">Items of a template</param>
    /// <param name="expected">The break item that was built for the write</param>
    public static int CountEqual(IEnumerable<ContainerTemplateItemResource> items, ContainerTemplateItemResource expected) =>
        items.Count(stored => IsEqual(stored, expected));

    /// <param name="template">Template whose visits are read</param>
    public static TimeOnly? LastVisitEnd(ContainerTemplateResource template)
    {
        var ends = template.ContainerTemplateItems
            .Where(item => item.ShiftId.HasValue && item.EndItem.HasValue)
            .Select(item => item.EndItem!.Value)
            .ToList();

        return ends.Count == 0 ? null : ends.Max();
    }

    private static bool IsEqual(ContainerTemplateItemResource stored, ContainerTemplateItemResource expected) =>
        stored.AbsenceId == expected.AbsenceId
        && !stored.ShiftId.HasValue
        && stored.StartItem == expected.StartItem
        && stored.EndItem == expected.EndItem
        && stored.TimeRangeStartItem == expected.TimeRangeStartItem
        && stored.TimeRangeEndItem == expected.TimeRangeEndItem;
}
