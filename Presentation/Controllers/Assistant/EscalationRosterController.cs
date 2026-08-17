// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Admin REST API for a group's escalation call list: lists the group's visible members, unfiltered
/// by absence or phone number. Wake-up order and reachability are computed on demand from
/// GroupVisibility/AppUser.DisplayOrder/UserAbsencePeriod; there is nothing here to reorder or persist -
/// the display order itself is maintained on AccountsController's Reorder endpoint.
/// </summary>
/// <param name="mediator">Dispatches the roster query.</param>

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.Assistant;

[ApiController]
[Route("api/backend/assistant/escalation-roster")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
public class EscalationRosterController : ControllerBase
{
    private readonly IMediator _mediator;

    public EscalationRosterController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EscalationRosterMemberResource>>> GetRoster([FromQuery] Guid groupId)
    {
        var result = await _mediator.Send(new GetEscalationRosterQuery(groupId));
        return Ok(result);
    }
}
