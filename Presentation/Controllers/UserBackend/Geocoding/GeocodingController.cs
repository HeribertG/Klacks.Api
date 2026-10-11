// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Server-side proxy for the free-text place search of the map, so the browser never calls the geocoder directly.
/// Open to any authenticated user (inherits BaseController's JWT scheme, no role) and rate limited per user,
/// because the geocoder's request slot is shared with address validation on save.
/// </summary>
/// <param name="query">Free-text place or address to look up</param>
/// <param name="language">Optional BCP 47 language tag for the returned display name</param>

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

    [HttpGet("Search")]
    [EnableRateLimiting(RateLimitingPolicies.GeoProxy)]
    public async Task<ActionResult<GeocodingSearchResource>> Search(
        [FromQuery] string? query,
        [FromQuery] string? language,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SearchGeocodingQuery(query ?? string.Empty, language), cancellationToken);
        return Ok(result);
    }
}
