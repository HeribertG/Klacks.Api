// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Write rules shared by the container skills that store visits placed by ContainerVisitScheduler
/// (optimize_container_route through ContainerRouteWriteService, bundle_nearby_timerange_shifts_into_container): which
/// placements can be stored, how a placed visit is written onto its template item, how the re-read template is
/// verified and how the placed visits are reported as stops.
/// </summary>

using System.Globalization;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Services.RouteOptimization.VisitScheduling;

namespace Klacks.Api.Application.Services.Schedules;

public static class ContainerVisitPlacement
{
    /// <summary>
    /// True when a travel time of a placed visit is 24 hours or longer and therefore cannot be stored as a clock time.
    /// </summary>
    /// <param name="scheduled">Placed visits</param>
    public static bool HasTravelOfADayOrMore(IEnumerable<ScheduledVisit> scheduled) =>
        scheduled.Any(v => v.TravelBeforeMinutes >= DayTimeConstants.MinutesPerDay
            || v.TravelAfterMinutes >= DayTimeConstants.MinutesPerDay);

    /// <summary>
    /// Writes the visit window and the travel before onto the item, and the travel after when the scheduler set one
    /// (otherwise the item keeps its stored travel after).
    /// </summary>
    /// <param name="visit">Placed visit</param>
    /// <param name="item">Template item of the visit's task shift</param>
    public static void ApplyTimes(ScheduledVisit visit, ContainerTemplateItemResource item)
    {
        item.TimeRangeStartItem = ContainerVisitScheduler.ClockTime(visit.StartMinutes);
        item.TimeRangeEndItem = ContainerVisitScheduler.ClockTime(visit.EndMinutes);
        item.TravelTimeBefore = ContainerVisitScheduler.ClockTime(visit.TravelBeforeMinutes);
        if (visit.TravelAfterMinutes.HasValue)
        {
            item.TravelTimeAfter = ContainerVisitScheduler.ClockTime(visit.TravelAfterMinutes.Value);
        }
    }

    /// <summary>
    /// True when the stored template has a RouteInfo and exactly the written visits with the written visit window and
    /// travel times before and after.
    /// </summary>
    /// <param name="written">Template as it was submitted</param>
    /// <param name="stored">Same template re-read after the write</param>
    public static bool StoredVisitTimesMatch(ContainerTemplateResource written, ContainerTemplateResource stored)
    {
        var expectedVisits = written.ContainerTemplateItems.Where(i => i.ShiftId.HasValue).ToList();
        var storedVisits = stored.ContainerTemplateItems.Where(i => i.ShiftId.HasValue).ToList();

        return stored.RouteInfo is not null
            && storedVisits.Count == expectedVisits.Count
            && expectedVisits.All(e => storedVisits.Any(s =>
                s.ShiftId == e.ShiftId
                && s.TimeRangeStartItem == e.TimeRangeStartItem
                && s.TimeRangeEndItem == e.TimeRangeEndItem
                && s.TravelTimeBefore == e.TravelTimeBefore
                && s.TravelTimeAfter == e.TravelTimeAfter));
    }

    /// <summary>
    /// The placed visits as numbered stops with the name and address the route step of their shift carries.
    /// </summary>
    /// <param name="visits">Placed visits in route order</param>
    /// <param name="steps">Route steps the visits were placed on</param>
    public static List<ContainerRouteStop> BuildStops(IEnumerable<ScheduledVisit> visits, IReadOnlyList<RouteStep> steps)
    {
        var stepByShiftId = steps
            .Where(s => s.ShiftId != Guid.Empty)
            .GroupBy(s => s.ShiftId)
            .ToDictionary(g => g.Key, g => g.First());

        return visits.Select((visit, index) =>
        {
            stepByShiftId.TryGetValue(visit.Item.ShiftId, out var step);
            return new ContainerRouteStop(
                index + 1,
                visit.Item.ShiftId,
                step?.Name ?? string.Empty,
                step?.Address ?? string.Empty,
                ClockText(visit.StartMinutes),
                ClockText(visit.EndMinutes),
                ClockText(visit.TravelBeforeMinutes));
        }).ToList();
    }

    private static string ClockText(int minutes) =>
        ContainerVisitScheduler.ClockTime(minutes).ToString(DateTimeFormats.HourMinute, CultureInfo.InvariantCulture);
}
