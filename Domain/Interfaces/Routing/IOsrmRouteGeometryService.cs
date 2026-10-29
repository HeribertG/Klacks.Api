// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves a route geometry from the configured OSRM instance, so the browser never talks to OSRM directly. The
/// profile follows the container transport mode (RoutingProfile.FromContainerTransportMode); an OSRM instance that only
/// serves the car profile (such as the public demo server) answers with car geometry for every profile. A disabled
/// routing service is answered without any outbound call.
/// </summary>
/// <param name="waypoints">Waypoints in visiting order; at least a start and a destination</param>
/// <param name="transportMode">Transport mode whose profile is routed (car, bicycle, foot; mix routes as car)</param>

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Interfaces.Routing;

public interface IOsrmRouteGeometryService
{
    Task<RouteGeometryResult> GetRouteGeometryAsync(
        IReadOnlyList<RoutePoint> waypoints, ContainerTransportMode transportMode, CancellationToken cancellationToken);
}
