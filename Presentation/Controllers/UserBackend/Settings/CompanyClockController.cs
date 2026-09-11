// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Read-only company time-zone/"today" endpoint for the UI. Open to any authenticated user (inherits
/// BaseController's JWT scheme, no Roles restriction) because GeneralSettingsController - the only
/// other place that can read the underlying APP_ADDRESS_TIMEZONE/APP_ADDRESS_COUNTRY settings - is
/// Admin-only, and calendar rendering needs the company zone for every user, not just admins.
/// </summary>
/// <param name="mediator">Dispatches GetCompanyClockQuery to ICompanyClock.</param>

using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Application.Queries.Settings.CompanyClock;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Settings;

public class CompanyClockController : BaseController
{
    private readonly IMediator _mediator;

    public CompanyClockController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<CompanyClockResource>> Get(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCompanyClockQuery(), cancellationToken);
        return Ok(result);
    }
}
