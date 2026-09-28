// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Interfaces.Routing;

public interface IRoutingService
{
    Task<IReadOnlyList<RoutePoint>?> GetRouteAsync(IReadOnlyList<RoutePoint> waypoints, CancellationToken cancellationToken);
}
