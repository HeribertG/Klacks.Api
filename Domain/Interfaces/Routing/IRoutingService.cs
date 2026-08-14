// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Interfaces.Routing;

public interface IRoutingService
{
    Task<IReadOnlyList<RoutePoint>?> GetRouteAsync(IReadOnlyList<RoutePoint> waypoints, CancellationToken cancellationToken);
}
