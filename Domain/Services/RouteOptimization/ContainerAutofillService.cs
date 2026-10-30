// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Automatic filling of container templates with matching TimeRange shifts.
/// Solves an Orienteering Problem in 3 phases: Greedy Selection, ACO+2-Opt, Post-Insertion.
/// Both entry points run the same ContainerAutofillPlanningStage; only the real run stores geocoded coordinates and
/// requests turn-by-turn directions.
/// </summary>
/// <param name="availableTasksService">Supplies the candidate tasks through the same query, and the same day rule
/// (ShiftDayActivityFilter), as the available-task list</param>
/// <param name="candidateLocator">Turns shifts into geocoded route candidates</param>
/// <param name="planningStage">The calculation from the candidates to the route result</param>
/// <param name="routeDirectionsBuilder">Retrieves turn-by-turn directions for the final route segments</param>

using Klacks.Api.Domain.Interfaces.RouteOptimization;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.RouteOptimization.ContainerAutofill;

namespace Klacks.Api.Domain.Services.RouteOptimization;

public class ContainerAutofillService : IContainerAutofillService
{
    private readonly IContainerAvailableTasksService _availableTasksService;
    private readonly ContainerAutofillCandidateLocator _candidateLocator;
    private readonly ContainerAutofillPlanningStage _planningStage;
    private readonly IRouteDirectionsBuilder _routeDirectionsBuilder;
    private readonly ILogger<ContainerAutofillService> _logger;

    public ContainerAutofillService(
        IContainerAvailableTasksService availableTasksService,
        ContainerAutofillCandidateLocator candidateLocator,
        ContainerAutofillPlanningStage planningStage,
        IRouteDirectionsBuilder routeDirectionsBuilder,
        ILogger<ContainerAutofillService> logger)
    {
        _availableTasksService = availableTasksService;
        _candidateLocator = candidateLocator;
        _planningStage = planningStage;
        _routeDirectionsBuilder = routeDirectionsBuilder;
        _logger = logger;
    }

    public async Task<ContainerAutofillResult> AutofillAsync(ContainerAutofillRequest request)
    {
        _logger.LogInformation(
            "Autofill started for Container: {ContainerId}, Weekday: {Weekday}, IsHoliday: {IsHoliday}, FromTime: {FromTime}, UntilTime: {UntilTime}",
            request.ContainerId, request.Weekday, request.IsHoliday, request.FromTime, request.UntilTime);

        _logger.LogInformation("Time budget: {TimeBudget} ({From} - {Until})", request.UntilTime - request.FromTime, request.FromTime, request.UntilTime);

        var availableTasks = await _availableTasksService.GetAvailableTasksAsync(
            request.ContainerId, request.Weekday, request.FromTime, request.UntilTime,
            isHoliday: request.IsHoliday,
            isWeekdayAndHoliday: request.IsWeekdayAndHoliday,
            additionalAvailableWorkIds: request.AdditionalAvailableWorkIds,
            restrictToShiftIds: request.RestrictToShiftIds,
            cancellationToken: request.CancellationToken);

        var timeRangeTasks = availableTasks.Where(s => s.IsTimeRange).ToList();
        _logger.LogInformation("Available tasks: {Total}, TimeRange tasks: {TimeRange}", availableTasks.Count, timeRangeTasks.Count);

        if (timeRangeTasks.Count == 0)
        {
            _logger.LogWarning("No TimeRange tasks available for autofill");
            return ContainerAutofillResultBuilder.CreateEmptyResult(request.TransportMode, availableTasks.Count);
        }

        return await AutofillTimeRangeTasksAsync(request, timeRangeTasks);
    }

    public async Task<ContainerAutofillPreview> PreviewAsync(ContainerAutofillPreviewRequest request)
    {
        var timeRangeTasks = request.Tasks.Where(s => s.IsTimeRange).ToList();
        if (timeRangeTasks.Count == 0)
        {
            return new ContainerAutofillPreview(
                ContainerAutofillPlanOutcome.NoTimeRangeTasks,
                ContainerAutofillResultBuilder.CreateEmptyResult(request.TransportMode),
                false, 0, [], []);
        }

        var located = await _candidateLocator.LocateAsync(
            timeRangeTasks, request.TransportMode, request.MaxGeocodedAddresses, request.CancellationToken);

        var plan = await _planningStage.PlanAsync(
            new ContainerAutofillPlanningInput(
                request.StartBase, request.EndBase, request.FromTime, request.UntilTime,
                request.TransportMode, request.TimeRangeTolerance, null, null, request.CancellationToken),
            located.Locations,
            timeRangeTasks.Count);

        var omissions = plan.Context is null
            ? []
            : ContainerAutofillOmissionClassifier.Classify(plan.Context, plan.Route, located.Locations);

        return new ContainerAutofillPreview(
            plan.Outcome, plan.Result, plan.TravelTimesEstimated, timeRangeTasks.Count, located.Unlocated, omissions);
    }

    private async Task<ContainerAutofillResult> AutofillTimeRangeTasksAsync(
        ContainerAutofillRequest request, List<Shift> timeRangeTasks)
    {
        var candidateLocations = await _candidateLocator.ExtractLocationsFromShiftsAsync(timeRangeTasks, request.TransportMode, request.CancellationToken);

        var plan = await _planningStage.PlanAsync(
            new ContainerAutofillPlanningInput(
                request.StartBase, request.EndBase, request.FromTime, request.UntilTime, request.TransportMode,
                request.TimeRangeTolerance, request.TimeBlocks, request.AdditionalAvailableWorkIds, request.CancellationToken),
            candidateLocations,
            timeRangeTasks.Count);

        if (plan is not { Outcome: ContainerAutofillPlanOutcome.Routed, Context: { } context })
        {
            return plan.Result;
        }

        var segmentDirections = await _routeDirectionsBuilder.GetRouteDirectionsAsync(plan.Result.OptimizedRoute, context.DistanceMatrix, request.TransportMode);
        return plan.Result with { SegmentDirections = segmentDirections, IsEstimated = context.DistanceMatrix.IsEstimated };
    }
}
