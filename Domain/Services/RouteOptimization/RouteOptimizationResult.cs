// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.RouteOptimization;

public record RouteOptimizationResult(
    List<Location> OptimizedRoute,
    double TotalDistanceKm,
    TimeSpan EstimatedTravelTime,
    double[,] DistanceMatrix,
    double[,] DurationMatrix,
    TimeSpan TravelTimeFromStartBase,
    List<int> RouteIndices,
    double DistanceFromStartBaseKm,
    double DistanceToEndBaseKm,
    TimeSpan TravelTimeToEndBase,
    List<int> FullRouteIndices,
    Dictionary<string, double[,]>? DurationMatricesByProfile = null,
    ContainerTransportMode TransportMode = ContainerTransportMode.ByCar,
    List<RouteSegmentDirections>? SegmentDirections = null,
    TimeSpan TotalBriefingDebriefingTime = default,
    List<PlacedTimeBlock>? PlacedTimeBlocks = null,
    bool IsEstimated = false);
