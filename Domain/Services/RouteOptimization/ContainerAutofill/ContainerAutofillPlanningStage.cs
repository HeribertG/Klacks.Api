// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The calculation of a container autofill, shared by the real run and the dry run: geocodes the bases, builds the
/// distance matrix with the minimum travel time, plans the route and builds the result. Stores nothing and requests no
/// turn-by-turn directions; geocoding of the bases and the matrix are reads against the external services.
/// </summary>
/// <param name="candidateLocator">Geocodes the base addresses</param>
/// <param name="routeOptimizationService">Service for distance matrix calculation</param>
/// <param name="travelTimeFloor">Raises short legs to the configured minimum travel time</param>
/// <param name="routePlanner">Runs greedy selection, ant colony reordering and post-insertion</param>

using Klacks.Api.Domain.Interfaces.RouteOptimization;
using Klacks.Api.Domain.Logging;

namespace Klacks.Api.Domain.Services.RouteOptimization.ContainerAutofill;

public sealed class ContainerAutofillPlanningStage
{
    private readonly ContainerAutofillCandidateLocator _candidateLocator;
    private readonly IRouteOptimizationService _routeOptimizationService;
    private readonly ContainerAutofillTravelTimeFloor _travelTimeFloor;
    private readonly ContainerAutofillRoutePlanner _routePlanner;
    private readonly ILogger<ContainerAutofillPlanningStage> _logger;

    public ContainerAutofillPlanningStage(
        ContainerAutofillCandidateLocator candidateLocator,
        IRouteOptimizationService routeOptimizationService,
        ContainerAutofillTravelTimeFloor travelTimeFloor,
        ContainerAutofillRoutePlanner routePlanner,
        ILogger<ContainerAutofillPlanningStage> logger)
    {
        _candidateLocator = candidateLocator;
        _routeOptimizationService = routeOptimizationService;
        _travelTimeFloor = travelTimeFloor;
        _routePlanner = routePlanner;
        _logger = logger;
    }

    /// <param name="input">Bases, time window and transport mode</param>
    /// <param name="candidateLocations">Located route candidates</param>
    /// <param name="totalAvailableShifts">Count reported in the result</param>
    public async Task<ContainerAutofillPlan> PlanAsync(
        ContainerAutofillPlanningInput input, List<Location> candidateLocations, int totalAvailableShifts)
    {
        if (candidateLocations.Count == 0)
        {
            _logger.LogWarning("No geocodable locations found");
            return Unrouted(input, ContainerAutofillPlanOutcome.NoLocatableTasks, totalAvailableShifts, null);
        }

        var locationsWithBases = await AppendBaseLocationsAsync(input, candidateLocations);
        if (locationsWithBases == null)
        {
            return Unrouted(input, ContainerAutofillPlanOutcome.BaseNotLocated, totalAvailableShifts, null);
        }

        var (allLocations, startBaseIndex, endBaseIndex) = locationsWithBases.Value;
        var allTimeBlocks = input.TimeBlocks ?? new List<TimeBlock>();
        var context = await BuildRouteContextAsync(input, allLocations, startBaseIndex, endBaseIndex, allTimeBlocks);

        var optimizedRoute = _routePlanner.PlanRoute(context, candidateLocations, input.AdditionalAvailableWorkIds);
        if (optimizedRoute == null)
        {
            return Unrouted(input, ContainerAutofillPlanOutcome.NothingSelected, totalAvailableShifts, context);
        }

        var result = ContainerAutofillResultBuilder.BuildResult(context, optimizedRoute, totalAvailableShifts, input.TransportMode, allTimeBlocks);
        return new ContainerAutofillPlan(
            ContainerAutofillPlanOutcome.Routed, result, context.DistanceMatrix.IsEstimated, context, optimizedRoute);
    }

    private static ContainerAutofillPlan Unrouted(
        ContainerAutofillPlanningInput input,
        ContainerAutofillPlanOutcome outcome,
        int totalAvailableShifts,
        ContainerAutofillRouteContext? context)
    {
        return new ContainerAutofillPlan(
            outcome,
            ContainerAutofillResultBuilder.CreateEmptyResult(input.TransportMode, totalAvailableShifts),
            context?.DistanceMatrix.IsEstimated ?? false,
            context,
            new List<int>());
    }

    private async Task<(List<Location> allLocations, int startBaseIndex, int endBaseIndex)?> AppendBaseLocationsAsync(
        ContainerAutofillPlanningInput input, List<Location> candidateLocations)
    {
        var startBaseLocation = await _candidateLocator.GeocodeBaseAddressAsync(input.StartBase, ContainerAutofillDefaults.StartBaseLabel, input.CancellationToken);
        var endBaseLocation = await _candidateLocator.GeocodeBaseAddressAsync(input.EndBase, ContainerAutofillDefaults.EndBaseLabel, input.CancellationToken);

        if (startBaseLocation == null || endBaseLocation == null)
        {
            _logger.LogWarning("Could not geocode base addresses. StartBase={StartBase}, EndBase={EndBase}", input.StartBase.ForLog(), input.EndBase.ForLog());
            return null;
        }

        var allLocations = new List<Location>(candidateLocations);
        var startBaseIndex = allLocations.Count;
        allLocations.Add(startBaseLocation);

        int endBaseIndex;
        if (input.StartBase == input.EndBase)
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
        ContainerAutofillPlanningInput input,
        List<Location> allLocations,
        int startBaseIndex,
        int endBaseIndex,
        List<TimeBlock> allTimeBlocks)
    {
        _logger.LogInformation("Building distance matrix for {Count} locations (including bases)", allLocations.Count);
        var distanceMatrix = await _routeOptimizationService.CalculateDistanceMatrixForLocationsAsync(allLocations, input.TransportMode);
        await _travelTimeFloor.ApplyAsync(distanceMatrix.DurationMatrix, input.TransportMode);

        var budgetSeconds = (input.UntilTime - input.FromTime).TotalSeconds;
        var unmovableBlocks = allTimeBlocks.Where(b => !b.IsMovable).ToList();
        var movableBlocks = allTimeBlocks.Where(b => b.IsMovable).ToList();

        var effectiveBudgetSeconds = TimeBlockScheduler.CalculateEffectiveBudget(budgetSeconds, allTimeBlocks);
        _logger.LogInformation("Time budget: {Total}s, effective (minus blocks): {Effective}s, unmovable: {Unmovable}, movable: {Movable}",
            budgetSeconds, effectiveBudgetSeconds, unmovableBlocks.Count, movableBlocks.Count);

        return new ContainerAutofillRouteContext(
            distanceMatrix, startBaseIndex, endBaseIndex,
            input.FromTime.ToTimeSpan().TotalSeconds, input.UntilTime.ToTimeSpan().TotalSeconds,
            effectiveBudgetSeconds, input.TimeRangeTolerance,
            unmovableBlocks, movableBlocks);
    }
}
