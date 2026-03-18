// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Builds distance and duration matrices using various routing services (OSRM, OpenRouteService) or Haversine fallback.
/// </summary>
/// <param name="locations">List of locations for which the matrix is calculated</param>
/// <param name="transportMode">The transport mode determining the routing profile</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Services.RouteOptimization;

namespace Klacks.Api.Domain.Interfaces.Staffs;

public interface IDistanceMatrixBuilder
{
    Task<(double[,] distanceMatrix, double[,] durationMatrix, Dictionary<string, double[,]>? durationMatricesByProfile)> BuildDistanceMatrixAsync(
        List<Location> locations,
        ContainerTransportMode transportMode);

    Task<(double[,] distanceMatrix, double[,] durationMatrix, Dictionary<string, double[,]> durationMatricesByProfile)> BuildMixedDistanceMatrixAsync(
        List<Location> locations);

    double[,] BuildHaversineDistanceMatrix(List<Location> locations);

    double[,] BuildEstimatedDurationMatrix(double[,] distanceMatrix, ContainerTransportMode transportMode);
}
