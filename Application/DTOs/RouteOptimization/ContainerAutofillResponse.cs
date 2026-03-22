// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Response DTO for the container autofill endpoint.
/// Contains the optimized route, selected shifts and time information.
/// </summary>
/// <param name="OptimizedRoute">Optimized order of locations as RouteStepDto</param>
/// <param name="SelectedShiftIds">IDs of the selected shifts</param>
/// <param name="TotalDistanceKm">Total distance in km</param>
/// <param name="EstimatedTravelTime">Estimated total time (travel + on-site)</param>
/// <param name="TotalWorkTime">Total on-site time (briefing + work + debriefing)</param>
/// <param name="RemainingTime">Remaining time in the container budget</param>

namespace Klacks.Api.Application.DTOs.RouteOptimization;

public class ContainerAutofillResponse
{
    public List<RouteStepDto> OptimizedRoute { get; set; } = new();
    public List<Guid> SelectedShiftIds { get; set; } = new();
    public double TotalDistanceKm { get; set; }
    public TimeSpan EstimatedTravelTime { get; set; }
    public TimeSpan TotalWorkTime { get; set; }
    public TimeSpan RemainingTime { get; set; }
    public int TotalAvailableShifts { get; set; }
    public int SelectedShiftCount { get; set; }
    public TimeSpan TravelTimeFromStartBase { get; set; }
    public double DistanceFromStartBaseKm { get; set; }
    public double DistanceToEndBaseKm { get; set; }
    public TimeSpan TravelTimeToEndBase { get; set; }
    public List<RouteSegmentDirectionsDto>? SegmentDirections { get; set; }
    public List<TimeBlockResultDto> PlacedTimeBlocks { get; set; } = new();
}
