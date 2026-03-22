// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Result of container autofill: Contains the optimized route and information about selected shifts.
/// </summary>
/// <param name="OptimizedRoute">Optimized order of locations</param>
/// <param name="SelectedShiftIds">IDs of the selected shifts</param>
/// <param name="TotalDistanceKm">Total distance in km</param>
/// <param name="EstimatedTravelTime">Estimated travel time</param>
/// <param name="TotalWorkTime">Total work time (briefing + work + debriefing)</param>
/// <param name="RemainingTime">Remaining time in the budget</param>
/// <param name="TotalAvailableShifts">Number of available shifts</param>
/// <param name="SelectedShiftCount">Number of selected shifts</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Services.RouteOptimization;

public record ContainerAutofillResult(
    List<Location> OptimizedRoute,
    List<Guid> SelectedShiftIds,
    double TotalDistanceKm,
    TimeSpan EstimatedTravelTime,
    TimeSpan TotalWorkTime,
    TimeSpan RemainingTime,
    int TotalAvailableShifts,
    int SelectedShiftCount,
    double[,] DistanceMatrix,
    double[,] DurationMatrix,
    TimeSpan TravelTimeFromStartBase,
    List<int> RouteIndices,
    double DistanceFromStartBaseKm,
    double DistanceToEndBaseKm,
    TimeSpan TravelTimeToEndBase,
    List<int> FullRouteIndices,
    Dictionary<string, double[,]>? DurationMatricesByProfile = null,
    Klacks.Api.Domain.Enums.ContainerTransportMode TransportMode = Klacks.Api.Domain.Enums.ContainerTransportMode.ByCar,
    List<RouteSegmentDirections>? SegmentDirections = null,
    TimeSpan TotalBriefingDebriefingTime = default,
    List<PlacedTimeBlock>? PlacedTimeBlocks = null);
