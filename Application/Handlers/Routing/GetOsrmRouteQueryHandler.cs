// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves a driving route geometry through the server-side OSRM proxy. The status tells the UI whether to draw
/// the route (Ok) or to fall back to straight lines (NoRoute, Disabled, Unavailable).
/// </summary>
/// <param name="request">Waypoints in visiting order; at least a start and a destination</param>

using Klacks.Api.Application.DTOs.Routing;
using Klacks.Api.Application.Queries.Routing;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Interfaces.Routing;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Routing;

public class GetOsrmRouteQueryHandler : IRequestHandler<GetOsrmRouteQuery, RouteGeometryResource>
{
    private readonly IOsrmRouteGeometryService _osrmRouteGeometryService;

    public GetOsrmRouteQueryHandler(IOsrmRouteGeometryService osrmRouteGeometryService)
    {
        _osrmRouteGeometryService = osrmRouteGeometryService;
    }

    public async Task<RouteGeometryResource> Handle(GetOsrmRouteQuery request, CancellationToken cancellationToken)
    {
        var waypoints = request.Coordinates
            .Select(c => new RoutePoint(c.Lat, c.Lon))
            .ToList();

        var result = await _osrmRouteGeometryService.GetRouteGeometryAsync(waypoints, cancellationToken);

        return new RouteGeometryResource
        {
            Status = result.Status,
            Points = result.Points
                .Select(p => new RoutePointResource { Lat = p.Lat, Lon = p.Lon })
                .ToList(),
        };
    }
}
