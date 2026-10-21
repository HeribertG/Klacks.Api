// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Automatic filling of container templates with matching TimeRange shifts.
/// Solves an Orienteering Problem in 3 phases: Greedy Selection, ACO+2-Opt, Post-Insertion.
/// Orchestrates the collaborators in ContainerAutofill/: candidates, distance matrix, travel time floor, route plan, result.
/// </summary>
/// <param name="availableTasksService">Supplies the candidate tasks through the same query, and the same day rule
/// (ShiftDayActivityFilter), as the available-task list</param>
/// <param name="candidateLocator">Turns shifts into geocoded route candidates and geocodes the bases</param>
/// <param name="routeOptimizationService">Service for distance matrix calculation</param>
/// <param name="travelTimeFloor">Raises short legs to the configured minimum travel time</param>
/// <param name="routePlanner">Runs greedy selection, ant colony reordering and post-insertion</param>
/// <param name="routeDirectionsBuilder">Retrieves turn-by-turn directions for the final route segments</param>

using Klacks.Api.Domain.Interfaces.RouteOptimization;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.RouteOptimization.ContainerAutofill;

namespace Klacks.Api.Domain.Services.RouteOptimization;

public class ContainerAutofillService : IContainerAutofillService
{
    private readonly IContainerAvailableTasksService _availableTasksService;
    private readonly ContainerAutofillCandidateLocator _candidateLocator;
    private readonly IRouteOptimizationService _routeOptimizationService;
    private readonly ContainerAutofillTravelTimeFloor _travelTimeFloor;
    private readonly ContainerAutofillRoutePlanner _routePlanner;
    private readonly IRouteDirectionsBuilder _routeDirectionsBuilder;
    private readonly ILogger<ContainerAutofillService> _logger;

    public ContainerAutofillService(
        IContainerAvailableTasksService availableTasksService,
        ContainerAutofillCandidateLocator candidateLocator,
        IRouteOptimizationService routeOptimizationService,
        ContainerAutofillTravelTimeFloor travelTimeFloor,
        ContainerAutofillRoutePlanner routePlanner,
        IRouteDirectionsBuilder routeDirectionsBuilder,
        ILogger<ContainerAutofillService> logger)
    {
        _availableTasksService = availableTasksService;
        _candidateLocator = candidateLocator;
        _routeOptimizationService = routeOptimizationService;
        _travelTimeFloor = travelTimeFloor;
        _routePlanner = routePlanner;
        _routeDirectionsBuilder = routeDirectionsBuilder;
        _logger = logger;
    }

    public async Task<ContainerAutofillResult> AutofillAsync(ContainerAutofillRequest request)
    {
        _logger.LogInformation(
            "Autofill started for Container: {ContainerId}, Weekday: {Weekday}, IsHoliday: {IsHoliday}, FromTime: {FromTime}, UntilTime: {UntilTime}",
            request.ContainerId, request.Weekday, request.IsHoliday, request.FromTime, request.UntilTime);

        var timeBudget = request.UntilTime - request.FromTime;
        _logger.LogInformation("Time budget: {TimeBudget} ({From} - {Until})", timeBudget, request.FromTime, request.UntilTime);

        var availableTasks = await _availableTasksService.GetAvailableTasksAsync(
            request.ContainerId, request.Weekday, request.FromTime, request.UntilTime,
            isHoliday: request.IsHoliday,
            additionalAvailableWorkIds: request.AdditionalAvailableWorkIds,
            cancellationToken: request.CancellationToken);

        var timeRangeTasks = availableTasks.Where(s => s.IsTimeRange).ToList();
        _logger.LogInformation("Available tasks: {Total}, TimeRange tasks: {TimeRange}", availableTasks.Count, timeRangeTasks.Count);

        if (timeRangeTasks.Count == 0)
        {
            _logger.LogWarning("No TimeRange tasks available for autofill");
            return ContainerAutofillResultBuilder.CreateEmptyResult(request.TransportMode, availableTasks.Count);
        }

        return await AutofillTimeRangeTasksAsync(request, timeRangeTasks, timeBudget);
    }

    private async Task<ContainerAutofillResult> AutofillTimeRangeTasksAsync(
        ContainerAutofillRequest request, List<Shift> timeRangeTasks, TimeSpan timeBudget)
    {
        var candidateLocations = await _candidateLocator.ExtractLocationsFromShiftsAsync(timeRangeTasks, request.TransportMode, request.CancellationToken);
        if (candidateLocations.Count == 0)
        {
            _logger.LogWarning("No geocodable locations found");
            return ContainerAutofillResultBuilder.CreateEmptyResult(request.TransportMode, timeRangeTasks.Count);
        }

        var locationsWithBases = await AppendBaseLocationsAsync(request, candidateLocations);
        if (locationsWithBases == null)
        {
            return ContainerAutofillResultBuilder.CreateEmptyResult(request.TransportMode, timeRangeTasks.Count);
        }

        var (allLocations, startBaseIndex, endBaseIndex) = locationsWithBases.Value;
        var allTimeBlocks = request.TimeBlocks ?? new List<TimeBlock>();
        var context = await BuildRouteContextAsync(request, allLocations, startBaseIndex, endBaseIndex, timeBudget, allTimeBlocks);

        var optimizedRoute = _routePlanner.PlanRoute(context, candidateLocations, request.AdditionalAvailableWorkIds);
        if (optimizedRoute == null)
        {
            return ContainerAutofillResultBuilder.CreateEmptyResult(request.TransportMode, timeRangeTasks.Count);
        }

        var result = ContainerAutofillResultBuilder.BuildResult(context, optimizedRoute, timeRangeTasks.Count, request.TransportMode, allTimeBlocks);
        var segmentDirections = await _routeDirectionsBuilder.GetRouteDirectionsAsync(result.OptimizedRoute, context.DistanceMatrix, request.TransportMode);
        return result with { SegmentDirections = segmentDirections, IsEstimated = context.DistanceMatrix.IsEstimated };
    }

    private async Task<(List<Location> allLocations, int startBaseIndex, int endBaseIndex)?> AppendBaseLocationsAsync(
        ContainerAutofillRequest request, List<Location> candidateLocations)
    {
        var startBaseLocation = await _candidateLocator.GeocodeBaseAddressAsync(request.StartBase, ContainerAutofillDefaults.StartBaseLabel, request.CancellationToken);
        var endBaseLocation = await _candidateLocator.GeocodeBaseAddressAsync(request.EndBase, ContainerAutofillDefaults.EndBaseLabel, request.CancellationToken);

        if (startBaseLocation == null || endBaseLocation == null)
        {
            _logger.LogWarning("Could not geocode base addresses. StartBase={StartBase}, EndBase={EndBase}", request.StartBase.ForLog(), request.EndBase.ForLog());
            return null;
        }

        var allLocations = new List<Location>(candidateLocations);
        var startBaseIndex = allLocations.Count;
        allLocations.Add(startBaseLocation);

        int endBaseIndex;
        if (request.StartBase == request.EndBase)
        {
            endBaseIndex = startBaseIndex;
        }
        else
        {
            endBaseIndex = allLocations.Count;
            allLocations.Add(endBaseLocation);
        }

        return (allLocations, startBaseIndex, endBaseIndex);
    }

    private async Task<ContainerAutofillRouteContext> BuildRouteContextAsync(
        ContainerAutofillRequest request,
        List<Location> allLocations,
        int startBaseIndex,
        int endBaseIndex,
        TimeSpan timeBudget,
        List<TimeBlock> allTimeBlocks)
    {
        _logger.LogInformation("Building distance matrix for {Count} locations (including bases)", allLocations.Count);
        var distanceMatrix = await _routeOptimizationService.CalculateDistanceMatrixForLocationsAsync(allLocations, request.TransportMode);
        await _travelTimeFloor.ApplyAsync(distanceMatrix.DurationMatrix, request.TransportMode);

        var budgetSeconds = timeBudget.TotalSeconds;
        var unmovableBlocks = allTimeBlocks.Where(b => !b.IsMovable).ToList();
        var movableBlocks = allTimeBlocks.Where(b => b.IsMovable).ToList();

        var effectiveBudgetSeconds = TimeBlockScheduler.CalculateEffectiveBudget(budgetSeconds, allTimeBlocks);
        _logger.LogInformation("Time budget: {Total}s, effective (minus blocks): {Effective}s, unmovable: {Unmovable}, movable: {Movable}",
            budgetSeconds, effectiveBudgetSeconds, unmovableBlocks.Count, movableBlocks.Count);

        return new ContainerAutofillRouteContext(
            distanceMatrix, startBaseIndex, endBaseIndex,
            request.FromTime.ToTimeSpan().TotalSeconds, request.UntilTime.ToTimeSpan().TotalSeconds,
            effectiveBudgetSeconds, request.TimeRangeTolerance,
            unmovableBlocks, movableBlocks);
    }
}
