// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Optimizes the route of one container weekday template and writes the result the way the Ui container page saves
/// it: route-optimize the visits (breaks as time blocks), place visit and travel times with ContainerVisitScheduler,
/// order breaks between the visits, store the RouteInfo and submit the container's complete template list under the
/// container template lock, then re-read and verify. OptimizeAndWriteAsync takes the lock itself;
/// OptimizeAndWriteUnderHeldLockAsync is for a caller that already holds it through a ContainerTemplateLockScope.
/// Unlike the Ui page, it writes nothing when the optimizer finds no route, when the same task is listed twice or when
/// a visit could not be placed (missing coordinates, shift not loaded), because a full-list write would delete those
/// visits, and nothing when a travel time does not fit a clock time.
/// </summary>
/// <param name="mediator">Reads and writes the container templates and takes the lock</param>
/// <param name="routeOptimizationService">Optimizes the visit order</param>
/// <param name="baseAddressResolver">Resolves the stored start and end base to an address</param>
/// <param name="scheduleMapper">Maps the RouteInfo to its resource</param>

using System.Globalization;
using Klacks.Api.Application.Commands.ContainerTemplates;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.ContainerTemplates;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Staffs;
using Klacks.Api.Domain.Services.RouteOptimization;
using Klacks.Api.Domain.Services.RouteOptimization.VisitScheduling;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Services.Schedules;

public class ContainerRouteWriteService : IContainerRouteWriteService
{
    private const int MinVisitsForRoute = 2;
    private const string TravelTimeFormat = @"hh\:mm";

    private readonly IMediator _mediator;
    private readonly IRouteOptimizationService _routeOptimizationService;
    private readonly IContainerBaseAddressResolver _baseAddressResolver;
    private readonly ScheduleMapper _scheduleMapper;

    public ContainerRouteWriteService(
        IMediator mediator,
        IRouteOptimizationService routeOptimizationService,
        IContainerBaseAddressResolver baseAddressResolver,
        ScheduleMapper scheduleMapper)
    {
        _mediator = mediator;
        _routeOptimizationService = routeOptimizationService;
        _baseAddressResolver = baseAddressResolver;
        _scheduleMapper = scheduleMapper;
    }

    public async Task<ContainerRouteWriteResult> OptimizeAndWriteAsync(
        Guid containerId, int storageWeekday, bool isHoliday, string instanceId, CancellationToken cancellationToken)
    {
        await using var lockScope = await ContainerTemplateLockScope.AcquireAsync(
            _mediator, containerId, instanceId, cancellationToken);
        return await OptimizeAndWriteUnderHeldLockAsync(lockScope, containerId, storageWeekday, isHoliday, cancellationToken);
    }

    public async Task<ContainerRouteWriteResult> OptimizeAndWriteUnderHeldLockAsync(
        ContainerTemplateLockScope lockScope, Guid containerId, int storageWeekday, bool isHoliday,
        CancellationToken cancellationToken)
    {
        if (!lockScope.Acquired)
        {
            return ContainerRouteWriteResult.Failure("The container is currently being edited by another user.");
        }

        var templates = await _mediator.Send(new GetContainerTemplatesQuery(containerId), cancellationToken);
        var template = templates.FirstOrDefault(t => t.Weekday == storageWeekday && t.IsHoliday == isHoliday);
        if (template is null)
        {
            return ContainerRouteWriteResult.Failure(
                "No container template is configured for this weekday" + (isHoliday ? " (holiday variant)." : "."));
        }

        var visitItems = template.ContainerTemplateItems.Where(i => i.ShiftId.HasValue).ToList();
        var absenceItems = template.ContainerTemplateItems.Where(i => i.AbsenceId.HasValue).ToList();
        if (visitItems.Count < MinVisitsForRoute)
        {
            return ContainerRouteWriteResult.Failure(
                $"At least {MinVisitsForRoute} visits are needed to optimize a route; the template has {visitItems.Count}.");
        }

        var plan = await PlanRouteAsync(template, visitItems, absenceItems, cancellationToken);
        if (plan.Error is not null)
        {
            return ContainerRouteWriteResult.Failure(plan.Error);
        }

        ApplyPlan(template, plan, absenceItems);

        try
        {
            await _mediator.Send(new PutContainerTemplatesCommand(containerId, templates), cancellationToken);
        }
        catch (ContainerLockedException ex)
        {
            return ContainerRouteWriteResult.Failure(ex.Message);
        }

        var verifyError = await VerifyAsync(containerId, template, cancellationToken);
        return verifyError is not null
            ? ContainerRouteWriteResult.Failure(verifyError)
            : BuildResult(plan, absenceItems.Count);
    }

    private sealed record RoutePlan(
        string? Error,
        string StartBase,
        string EndBase,
        RouteOptimizationResult? Optimization,
        List<RouteStep> Steps,
        List<(ScheduledVisit Visit, ContainerTemplateItemResource Item)> Placed);

    private async Task<RoutePlan> PlanRouteAsync(
        ContainerTemplateResource template,
        List<ContainerTemplateItemResource> visitItems,
        List<ContainerTemplateItemResource> absenceItems,
        CancellationToken cancellationToken)
    {
        if (visitItems.Select(i => i.ShiftId!.Value).Distinct().Count() != visitItems.Count)
        {
            return Failed("The template lists the same task more than once. Nothing was written, because saving would keep only one of them.");
        }

        var startBase = await _baseAddressResolver.ResolveAsync(template.StartBase, cancellationToken);
        var endBase = await _baseAddressResolver.ResolveAsync(template.EndBase, cancellationToken);
        var absences = absenceItems.Select(ToAbsenceItem).ToList();

        var optimization = await _routeOptimizationService.OptimizeRouteByShiftIdsAsync(
            visitItems.Select(i => i.ShiftId!.Value).ToList(),
            startBase.Length > 0 ? startBase : null,
            endBase.Length > 0 ? endBase : null,
            template.TransportMode,
            ContainerAbsenceTimeBlocks.BuildTimeBlocks(absences),
            template.FromTime);

        if (optimization.OptimizedRoute.Count == 0)
        {
            return Failed("No route could be optimized: fewer than two visits have an address with coordinates. Nothing was written.");
        }

        var steps = RouteStepBuilder.FromOptimization(optimization);
        var scheduled = ContainerVisitScheduler.Schedule(new VisitScheduleInput(
            ContainerVisitScheduler.ClockMinutes(template.FromTime),
            steps,
            optimization.TravelTimeFromStartBase,
            optimization.TravelTimeToEndBase,
            optimization.DistanceToEndBaseKm,
            visitItems.Select(ToVisitItem).ToList(),
            ContainerAbsenceTimeBlocks.BuildFixedIntervals(absences)));

        if (scheduled.Count != visitItems.Count)
        {
            return Failed(
                $"Only {scheduled.Count} of {visitItems.Count} visits could be placed on the route (a visit without address " +
                "coordinates or without its task shift). Nothing was written, because saving would remove the others.");
        }

        if (scheduled.Any(v => v.TravelBeforeMinutes >= DayTimeConstants.MinutesPerDay
            || v.TravelAfterMinutes >= DayTimeConstants.MinutesPerDay))
        {
            return Failed("A computed travel time is 24 hours or longer and cannot be stored. Nothing was written.");
        }

        var lastItemByShiftId = new Dictionary<Guid, ContainerTemplateItemResource>();
        foreach (var item in visitItems)
        {
            lastItemByShiftId[item.ShiftId!.Value] = item;
        }

        var placed = scheduled.Select(v => (v, lastItemByShiftId[v.Item.ShiftId])).ToList();
        return new RoutePlan(null, startBase, endBase, optimization, steps, placed);

        static RoutePlan Failed(string error) => new(error, string.Empty, string.Empty, null, [], []);
    }

    private void ApplyPlan(
        ContainerTemplateResource template, RoutePlan plan, List<ContainerTemplateItemResource> absenceItems)
    {
        foreach (var (visit, item) in plan.Placed)
        {
            item.TimeRangeStartItem = ContainerVisitScheduler.ClockTime(visit.StartMinutes);
            item.TimeRangeEndItem = ContainerVisitScheduler.ClockTime(visit.EndMinutes);
            item.TravelTimeBefore = ContainerVisitScheduler.ClockTime(visit.TravelBeforeMinutes);
            if (visit.TravelAfterMinutes.HasValue)
            {
                item.TravelTimeAfter = ContainerVisitScheduler.ClockTime(visit.TravelAfterMinutes.Value);
            }
        }

        template.ContainerTemplateItems = ContainerTemplateItemOrder.Interleave(
            absenceItems, plan.Placed.Select(p => p.Item).ToList(), ContainerTemplateItemStart.Minutes);
        template.RouteInfo = _scheduleMapper.ToRouteInfoResource(
            ContainerRouteInfoBuilder.Build(plan.StartBase, plan.EndBase, plan.Optimization!, plan.Steps));
    }

    private async Task<string?> VerifyAsync(
        Guid containerId, ContainerTemplateResource written, CancellationToken cancellationToken)
    {
        var reread = await _mediator.Send(new GetContainerTemplatesQuery(containerId), cancellationToken);
        var stored = reread.FirstOrDefault(t =>
            t.Weekday == written.Weekday
            && t.IsHoliday == written.IsHoliday
            && t.IsWeekdayAndHoliday == written.IsWeekdayAndHoliday);
        if (stored is null)
        {
            return "Verification failed: the container template could not be re-read after the write.";
        }

        var expectedVisits = written.ContainerTemplateItems.Where(i => i.ShiftId.HasValue).ToList();
        var storedVisits = stored.ContainerTemplateItems.Where(i => i.ShiftId.HasValue).ToList();
        var allPresent = stored.RouteInfo is not null
            && storedVisits.Count == expectedVisits.Count
            && expectedVisits.All(e => storedVisits.Any(s =>
                s.ShiftId == e.ShiftId
                && s.TimeRangeStartItem == e.TimeRangeStartItem
                && s.TimeRangeEndItem == e.TimeRangeEndItem
                && s.TravelTimeBefore == e.TravelTimeBefore
                && s.TravelTimeAfter == e.TravelTimeAfter));

        return allPresent
            ? null
            : "Verification failed: the planned visit and travel times or the route info could not be confirmed in the container template after the write.";
    }

    private static ContainerRouteWriteResult BuildResult(RoutePlan plan, int breakCount)
    {
        var stepByShiftId = plan.Steps
            .Where(s => s.ShiftId != Guid.Empty)
            .GroupBy(s => s.ShiftId)
            .ToDictionary(g => g.Key, g => g.First());

        var stops = plan.Placed.Select((p, index) =>
        {
            stepByShiftId.TryGetValue(p.Visit.Item.ShiftId, out var step);
            return new ContainerRouteStop(
                index + 1,
                p.Visit.Item.ShiftId,
                step?.Name ?? string.Empty,
                step?.Address ?? string.Empty,
                ClockText(p.Visit.StartMinutes),
                ClockText(p.Visit.EndMinutes),
                ClockText(p.Visit.TravelBeforeMinutes));
        }).ToList();

        var optimization = plan.Optimization!;
        return new ContainerRouteWriteResult(
            null,
            plan.StartBase,
            plan.EndBase,
            optimization.TotalDistanceKm,
            optimization.EstimatedTravelTime.ToString(TravelTimeFormat, CultureInfo.InvariantCulture),
            optimization.IsEstimated,
            stops,
            breakCount);
    }

    private static string ClockText(int minutes) =>
        ContainerVisitScheduler.ClockTime(minutes).ToString(DateTimeFormats.HourMinute, CultureInfo.InvariantCulture);

    private static VisitItem ToVisitItem(ContainerTemplateItemResource item) => new(
        item.ShiftId!.Value,
        item.Shift is not null,
        (double)(item.Shift?.WorkTime ?? 0m),
        item.BriefingTime,
        item.DebriefingTime,
        item.TransportMode);

    private static ContainerAbsenceItem ToAbsenceItem(ContainerTemplateItemResource item) => new(
        item.AbsenceId!.Value,
        item.StartItem,
        item.EndItem,
        item.TimeRangeStartItem,
        item.TimeRangeEndItem);
}
