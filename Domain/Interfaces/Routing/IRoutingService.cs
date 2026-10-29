// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Interfaces.Routing;

/// <summary>
/// Resolves a road route geometry through OpenRouteService with the profile of a container transport mode
/// (RoutingProfile.OpenRouteServiceDirectionsGeoJsonPath); null when no route can be obtained.
/// </summary>
/// <param name="waypoints">Waypoints in visiting order; at least a start and a destination</param>
/// <param name="transportMode">Transport mode whose profile is routed (car, bicycle, foot; mix routes as car)</param>
public interface IRoutingService
{
    Task<IReadOnlyList<RoutePoint>?> GetRouteAsync(
        IReadOnlyList<RoutePoint> waypoints, ContainerTransportMode transportMode, CancellationToken cancellationToken);
}
