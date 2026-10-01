// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Builds distance and duration matrices using various routing services (OSRM, OpenRouteService) or Haversine fallback.
/// The returned matrix is flagged as estimated whenever the Haversine fallback had to be used.
/// </summary>
/// <param name="locations">List of locations for which the matrix is calculated</param>
/// <param name="transportMode">The transport mode determining the routing profile</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Services.RouteOptimization;

namespace Klacks.Api.Domain.Interfaces.Staffs;

public interface IDistanceMatrixBuilder
{
    Task<DistanceMatrix> BuildDistanceMatrixAsync(
        List<Location> locations,
        ContainerTransportMode transportMode);

    Task<DistanceMatrix> BuildMixedDistanceMatrixAsync(
        List<Location> locations);

    double[,] BuildHaversineDistanceMatrix(List<Location> locations);

    double[,] BuildEstimatedDurationMatrix(double[,] distanceMatrix, ContainerTransportMode transportMode);
}
