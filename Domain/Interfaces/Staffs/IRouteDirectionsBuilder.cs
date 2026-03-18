// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Retrieves turn-by-turn route directions for segments between locations using OSRM.
/// </summary>
/// <param name="fullRoute">Ordered list of locations forming the complete route</param>
/// <param name="distanceMatrix">The distance matrix containing all location data</param>
/// <param name="containerTransportMode">The container-level transport mode</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Services.RouteOptimization;

namespace Klacks.Api.Domain.Interfaces.Staffs;

public interface IRouteDirectionsBuilder
{
    Task<List<RouteSegmentDirections>> GetRouteDirectionsAsync(
        List<Location> fullRoute,
        DistanceMatrix distanceMatrix,
        ContainerTransportMode containerTransportMode);
}
