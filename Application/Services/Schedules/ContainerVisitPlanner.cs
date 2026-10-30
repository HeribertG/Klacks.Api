// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Places the visits on an autofill route exactly as the Ui does after an autofill and orders the template items by the
/// route: the autofill route becomes route steps (RouteStepBuilder.FromAutofill), ContainerVisitScheduler places the visit
/// windows and the travel times before and after in route order, and the route info is built for the template. Refuses when
/// a picked visit cannot be placed or a travel time does not fit a clock time, because the stored template would then not
/// match the route: Plan throws a SkillVerificationException for the skills that write, TryPlan answers with the refusal text
/// for the advice that writes nothing, so both judge a route by the same rules.
/// </summary>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.RouteOptimization;
using Klacks.Api.Domain.Services.RouteOptimization.VisitScheduling;

namespace Klacks.Api.Application.Services.Schedules;

public static class ContainerVisitPlanner
{
    /// <param name="skillName">Skill that asked, carried on a refusal</param>
    /// <param name="location">Base address the route starts and ends at</param>
    /// <param name="fromTime">Start of the day's time budget</param>
    /// <param name="autofillResult">Visible autofill selection with its route</param>
    /// <param name="taskShifts">Picked task shifts in selection order</param>
    public static PlannedContainerVisits Plan(
        string skillName, string location, TimeOnly fromTime, ContainerAutofillResult autofillResult, List<Shift> taskShifts)
    {
        var (visits, refusal) = TryPlan(location, fromTime, autofillResult, taskShifts);
        return visits ?? throw new SkillVerificationException(skillName, refusal!);
    }

    /// <param name="location">Base address the route starts and ends at</param>
    /// <param name="fromTime">Start of the day's time budget</param>
    /// <param name="autofillResult">Visible autofill selection with its route</param>
    /// <param name="taskShifts">Picked task shifts in selection order</param>
    /// <returns>The placed visits, or null and the reason the route would be refused</returns>
    public static (PlannedContainerVisits? Visits, string? Refusal) TryPlan(
        string location, TimeOnly fromTime, ContainerAutofillResult autofillResult, List<Shift> taskShifts)
    {
        var itemByShiftId = taskShifts.ToDictionary(shift => shift.Id, BuildTemplateItem);
        var steps = RouteStepBuilder.FromAutofill(autofillResult);
        var scheduled = ContainerVisitScheduler.Schedule(new VisitScheduleInput(
            ContainerVisitScheduler.ClockMinutes(fromTime),
            steps,
            autofillResult.TravelTimeFromStartBase,
            autofillResult.TravelTimeToEndBase,
            autofillResult.DistanceToEndBaseKm,
            taskShifts.Select(ToVisitItem).ToList(),
            Array.Empty<FixedTimeInterval>()));

        if (scheduled.Count != taskShifts.Count)
        {
            return (null,
                $"Only {scheduled.Count} of {taskShifts.Count} selected services could be placed on the route, so no " +
                "visit times could be planned — no container was created.");
        }

        if (ContainerVisitPlacement.HasTravelOfADayOrMore(scheduled))
        {
            return (null, "A computed travel time is 24 hours or longer and cannot be stored — no container was created.");
        }

        var items = scheduled.Select(visit =>
        {
            var item = itemByShiftId[visit.Item.ShiftId];
            ContainerVisitPlacement.ApplyTimes(visit, item);
            return item;
        }).ToList();

        var routeInfo = ContainerRouteInfoBuilder.Build(location, location, autofillResult, steps);
        return (new PlannedContainerVisits(items, ContainerVisitPlacement.BuildStops(scheduled, steps), routeInfo), null);
    }

    private static VisitItem ToVisitItem(Shift taskShift) => new(
        taskShift.Id,
        true,
        (double)taskShift.WorkTime,
        taskShift.BriefingTime,
        taskShift.DebriefingTime,
        Klacks.Api.Domain.Enums.TransportMode.ByCar);

    private static ContainerTemplateItemResource BuildTemplateItem(Shift taskShift)
    {
        return new ContainerTemplateItemResource
        {
            Id = Guid.Empty,
            ShiftId = taskShift.Id,
            AbsenceId = null,
            StartItem = taskShift.StartShift,
            EndItem = taskShift.EndShift,
            BriefingTime = taskShift.BriefingTime,
            DebriefingTime = taskShift.DebriefingTime,
            TravelTimeAfter = taskShift.TravelTimeAfter,
            TravelTimeBefore = taskShift.TravelTimeBefore,
            TimeRangeStartItem = null,
            TimeRangeEndItem = null,
            TransportMode = Klacks.Api.Domain.Enums.TransportMode.ByCar
        };
    }
}
