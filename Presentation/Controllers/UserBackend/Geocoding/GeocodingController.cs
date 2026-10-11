// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Server-side proxy for the free-text place search of the map, so the browser never calls the geocoder directly.
/// Open to any authenticated user (inherits BaseController's JWT scheme, no role) and rate limited per user,
/// because the geocoder's request slot is shared with address validation on save. The search term travels in the
/// POST body, so it never ends up in the reverse proxy's access log.
/// </summary>
/// <param name="request">JSON body: free-text place or address (query) and an optional BCP 47 language tag</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Geocoding;
using Klacks.Api.Application.Queries.Geocoding;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Geocoding;

public class GeocodingController : BaseController
{
    private readonly IMediator _mediator;

    public GeocodingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("Search")]
    [EnableRateLimiting(RateLimitingPolicies.GeoProxy)]
    public async Task<ActionResult<GeocodingSearchResource>> Search(
        [FromBody] GeocodingSearchRequest? request,
        CancellationToken cancellationToken)
    {
        var query = new SearchGeocodingQuery(request?.Query ?? string.Empty, request?.Language);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
