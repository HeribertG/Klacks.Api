// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.RouteOptimization;

public class RouteOptimizationResponse
{
    public List<RouteStepDto> OptimizedRoute { get; set; } = new();

    public double TotalDistanceKm { get; set; }

    public TimeSpan EstimatedTravelTime { get; set; }

    public TimeSpan TravelTimeFromStartBase { get; set; }

    public double DistanceFromStartBaseKm { get; set; }

    public double DistanceToEndBaseKm { get; set; }

    public TimeSpan TravelTimeToEndBase { get; set; }

    public List<RouteSegmentDirectionsDto>? SegmentDirections { get; set; }
    public List<TimeBlockResultDto> PlacedTimeBlocks { get; set; } = new();

    public bool IsEstimated { get; set; }
}
