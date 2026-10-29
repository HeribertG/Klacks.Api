// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Places the visits of a container day along an optimized route: travel, briefing, work and debriefing one after
/// the other from the container start, each visit pushed past an overlapping fixed break and the travel split around
/// a break in between. A port of the Ui ContainerTemplateItemManipulationService.applyOptimizedRoute with the same
/// results (pinned by the shared container-visit-schedule-cases.json), including its quirks: travel is read from the
/// TimeSpan wire text (seconds truncated, a day prefix read as hours), a route step without a visit still advances the
/// step index, items without a loaded shift or missing from the route are not scheduled, a duplicate shift id keeps
/// the last item, breaks are checked in one forward pass, and the return leg is written only for a positive distance.
/// </summary>
/// <param name="input">Container start, route, return leg, visits and sorted fixed breaks</param>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Services.RouteOptimization.VisitScheduling;

public static class ContainerVisitScheduler
{
    public static List<ScheduledVisit> Schedule(VisitScheduleInput input)
    {
        var itemsByShiftId = new Dictionary<Guid, VisitItem>();
        foreach (var visit in input.Visits)
        {
            itemsByShiftId[visit.ShiftId] = visit;
        }

        var scheduled = new List<ScheduledVisit>();
        var currentStartMinutes = input.ContainerStartMinutes;

        for (var i = 0; i < input.Route.Count; i++)
        {
            if (!itemsByShiftId.TryGetValue(input.Route[i].ShiftId, out var item) || !item.HasShift)
            {
                continue;
            }

            var travelToVisit = i == 0
                ? TravelMinutes(input.TravelTimeFromStartBase, item.TransportMode)
                : TravelMinutes(input.Route[i - 1].TravelTimeToNext, item.TransportMode);

            var briefingMinutes = ClockMinutes(item.BriefingTime);
            var debriefingMinutes = ClockMinutes(item.DebriefingTime);
            var workMinutes = WorkMinutes(item.WorkTimeHours);

            var shiftStart = currentStartMinutes + travelToVisit + briefingMinutes;
            var adjustedStart = ResolveAbsenceConflict(shiftStart, shiftStart + workMinutes, input.Absences);
            var adjustedEnd = adjustedStart + workMinutes;

            var travelBefore = travelToVisit;
            var absenceBetween = FindAbsenceBetween(currentStartMinutes, adjustedStart, input.Absences);
            if (absenceBetween != null && scheduled.Count > 0)
            {
                var travelAfterPrevious = Math.Max(0, absenceBetween.StartMinutes - currentStartMinutes);
                travelBefore = Math.Max(0, adjustedStart - briefingMinutes - absenceBetween.EndMinutes);
                scheduled[^1] = scheduled[^1] with { TravelAfterMinutes = travelAfterPrevious };
            }

            scheduled.Add(new ScheduledVisit(item, adjustedStart, adjustedEnd, travelBefore, null));
            currentStartMinutes = adjustedStart + workMinutes + debriefingMinutes;
        }

        if (input.DistanceToEndBaseKm > 0 && scheduled.Count > 0)
        {
            var last = scheduled[^1];
            scheduled[^1] = last with
            {
                TravelAfterMinutes = TravelMinutes(input.TravelTimeToEndBase, last.Item.TransportMode)
            };
        }

        return scheduled;
    }

    /// <summary>
    /// Minutes since midnight of a clock time; seconds are ignored.
    /// </summary>
    public static int ClockMinutes(TimeOnly time) => time.Hour * DayTimeConstants.MinutesPerHour + time.Minute;

    /// <summary>
    /// Clock time of a minute count, wrapped into one day.
    /// </summary>
    public static TimeOnly ClockTime(int minutes)
    {
        var minutesOfDay = minutes % DayTimeConstants.MinutesPerDay;
        return new TimeOnly(minutesOfDay / DayTimeConstants.MinutesPerHour, minutesOfDay % DayTimeConstants.MinutesPerHour);
    }

    /// <summary>
    /// Work time in minutes, hours x 60 rounded half up.
    /// </summary>
    public static int WorkMinutes(double workTimeHours)
    {
        var minutes = workTimeHours * DayTimeConstants.MinutesPerHour;
        var floor = Math.Floor(minutes);
        return (int)(minutes - floor >= ContainerVisitDefaults.RoundingMidpoint ? floor + 1 : floor);
    }

    /// <summary>
    /// Travel minutes read from the TimeSpan wire text ("c" format) exactly like the Ui reads it: the first three
    /// colon parts as integers (hours, minutes, whole seconds), rounded up to whole minutes and lifted to the minimum
    /// of the transport mode when above zero.
    /// </summary>
    /// <param name="travelTime">Travel time of the leg</param>
    /// <param name="transportMode">Transport mode deciding the minimum travel time</param>
    public static int TravelMinutes(TimeSpan travelTime, TransportMode transportMode)
    {
        var parts = travelTime
            .ToString(ContainerVisitDefaults.TimeSpanWireFormat, CultureInfo.InvariantCulture)
            .Split(ContainerVisitDefaults.TimeSpanPartSeparator);

        var hours = LeadingInteger(parts[ContainerVisitDefaults.TimeSpanHoursPart]);
        var minutes = LeadingInteger(parts[ContainerVisitDefaults.TimeSpanMinutesPart]);
        var seconds = parts.Length > ContainerVisitDefaults.TimeSpanSecondsPart
            ? LeadingInteger(parts[ContainerVisitDefaults.TimeSpanSecondsPart])
            : 0;
        double totalMinutes = hours * DayTimeConstants.MinutesPerHour + minutes;
        totalMinutes += seconds / (double)DayTimeConstants.SecondsPerMinute;

        return totalMinutes > 0
            ? Math.Max(MinTravelMinutes(transportMode), (int)Math.Ceiling(totalMinutes))
            : 0;
    }

    private static int MinTravelMinutes(TransportMode transportMode) => transportMode switch
    {
        TransportMode.ByBicycle => ContainerVisitDefaults.MinTravelMinutesByBicycle,
        TransportMode.ByFoot => ContainerVisitDefaults.MinTravelMinutesByFoot,
        _ => ContainerVisitDefaults.MinTravelMinutesByCar
    };

    private static int LeadingInteger(string text)
    {
        var length = 0;
        if (length < text.Length && (text[length] == ContainerVisitDefaults.SignMinus || text[length] == ContainerVisitDefaults.SignPlus))
        {
            length++;
        }

        while (length < text.Length && char.IsAsciiDigit(text[length]))
        {
            length++;
        }

        return int.Parse(text.AsSpan(0, length), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
    }

    private static FixedTimeInterval? FindAbsenceBetween(
        int windowStart, int windowEnd, IReadOnlyList<FixedTimeInterval> absences)
    {
        return absences.FirstOrDefault(a => a.StartMinutes >= windowStart && a.StartMinutes < windowEnd);
    }

    private static int ResolveAbsenceConflict(int shiftStart, int shiftEnd, IReadOnlyList<FixedTimeInterval> absences)
    {
        var adjustedStart = shiftStart;
        var duration = shiftEnd - shiftStart;

        foreach (var absence in absences)
        {
            var adjustedEnd = adjustedStart + duration;
            if (adjustedStart < absence.EndMinutes && adjustedEnd > absence.StartMinutes)
            {
                adjustedStart = absence.EndMinutes;
            }
        }

        return adjustedStart;
    }
}
