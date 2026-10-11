// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves a driving route geometry from the configured OSRM instance, so the browser never talks to OSRM
/// directly. A disabled routing service is answered without any outbound call.
/// </summary>
/// <param name="waypoints">Waypoints in visiting order; at least a start and a destination</param>

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Interfaces.Routing;

public interface IOsrmRouteGeometryService
{
    Task<RouteGeometryResult> GetRouteGeometryAsync(IReadOnlyList<RoutePoint> waypoints, CancellationToken cancellationToken);
}
