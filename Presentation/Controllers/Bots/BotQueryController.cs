// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Read-only query endpoints for the external Otto bot. Deliberately does NOT inherit
/// BaseController: BaseController pins the JWT scheme at the class level, and stacking a
/// second [Authorize(AuthenticationSchemes=...)] attribute on a derived class unions the
/// accepted schemes rather than replacing them, which would let any ordinary JWT-authenticated
/// user reach this controller too. This controller accepts only the KlacksBotToken scheme.
/// </summary>
using Klacks.Api.Application.DTOs.Bots;
using Klacks.Api.Application.Queries.Bots;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Klacks.Api.Presentation.Controllers.Bots;

[ApiController]
[Route("api/backend/bot")]
[Authorize(AuthenticationSchemes = KlacksBotTokenConstants.SchemeName)]
[EnableRateLimiting(RateLimitingPolicies.BotQuery)]
public class BotQueryController : ControllerBase
{
    private readonly IMediator _mediator;

    public BotQueryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("unstaffed-shifts")]
    public async Task<ActionResult<UnstaffedShiftSummaryDto>> GetUnstaffedShifts(
        [FromQuery] string clientName,
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate)
    {
        var result = await _mediator.Send(new GetUnstaffedShiftSummaryQuery(clientName, startDate, endDate));
        return Ok(result);
    }
}
