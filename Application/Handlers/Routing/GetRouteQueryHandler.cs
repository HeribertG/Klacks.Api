// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Routing;
using Klacks.Api.Application.Queries.Routing;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Interfaces.Routing;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Routing;

/// <summary>
/// Resolves a driving route through the server-side OpenRouteService proxy.
/// </summary>
/// <param name="request">Waypoints in visiting order; at least a start and a destination</param>
public class GetRouteQueryHandler : IRequestHandler<GetRouteQuery, List<RoutePointResource>?>
{
    private readonly IRoutingService _routingService;

    public GetRouteQueryHandler(IRoutingService routingService)
    {
        _routingService = routingService;
    }

    public async Task<List<RoutePointResource>?> Handle(GetRouteQuery request, CancellationToken cancellationToken)
    {
        var waypoints = request.Coordinates
            .Select(c => new RoutePoint(c.Lat, c.Lon))
            .ToList();

        var route = await _routingService.GetRouteAsync(waypoints, cancellationToken);

        return route?
            .Select(p => new RoutePointResource { Lat = p.Lat, Lon = p.Lon })
            .ToList();
    }
}
