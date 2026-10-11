// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Routing;
using Klacks.Api.Application.Queries.Routing;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Routing;

/// <summary>
/// Server-side proxy for map route geometries, so the OpenRouteService API key never reaches a browser and the
/// browser never calls OSRM directly. OsrmRoute answers 200 with a status (Ok, NoRoute, Disabled, Unavailable).
/// </summary>
/// <param name="coordinates">Waypoints in visiting order; at least a start and a destination</param>
[ApiController]
public class RoutingController : BaseController
{
    private readonly IMediator _mediator;

    public RoutingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("Directions")]
    public async Task<ActionResult<IEnumerable<RoutePointResource>>> Directions(
        [FromBody] List<RoutePointResource> coordinates,
        CancellationToken cancellationToken)
    {
        var route = await _mediator.Send(new GetRouteQuery(coordinates), cancellationToken);

        if (route == null)
        {
            return NoContent();
        }

        return Ok(route);
    }

    [HttpPost("OsrmRoute")]
    [EnableRateLimiting(RateLimitingPolicies.GeoProxy)]
    public async Task<ActionResult<RouteGeometryResource>> OsrmRoute(
        [FromBody] List<RoutePointResource> coordinates,
        CancellationToken cancellationToken)
    {
        var route = await _mediator.Send(new GetOsrmRouteQuery(coordinates), cancellationToken);
        return Ok(route);
    }
}
