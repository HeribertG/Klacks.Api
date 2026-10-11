// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Read-only geo service configuration for the UI (which map, search and routing features are available). Open to
/// any authenticated user (inherits BaseController's JWT scheme, no role) because every map view needs it, and it
/// carries no personal data and no internal host names.
/// </summary>
/// <param name="mediator">Dispatches GetGeoServicesConfigQuery to the geo service policy.</param>

using Klacks.Api.Application.DTOs.Settings.GeoServices;
using Klacks.Api.Application.Queries.Settings.GeoServices;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Settings;

public class GeoServicesConfigController : BaseController
{
    private readonly IMediator _mediator;

    public GeoServicesConfigController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<GeoServicesConfigResource>> Get(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetGeoServicesConfigQuery(), cancellationToken);
        return Ok(result);
    }
}
