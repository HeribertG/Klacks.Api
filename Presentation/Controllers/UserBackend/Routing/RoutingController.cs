// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Routing;
using Klacks.Api.Application.Queries.Routing;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Routing;

/// <summary>
/// Server-side proxy for map route geometries, so the OpenRouteService API key never reaches a browser.
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
}
