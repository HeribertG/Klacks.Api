// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Admin REST API for the escalation roster: one flat, group-agnostic list of every user with any
/// GroupVisibility and a phone number, and the endpoint to drag'n'drop their wake-up order
/// (AppUser.EscalationRosterOrder - a column dedicated to this domain, never the user administration
/// list's DisplayOrder).
/// </summary>
/// <param name="mediator">Dispatches the roster query and reorder command.</param>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.DTOs;
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
    public async Task<ActionResult<IReadOnlyList<EscalationRosterMemberResource>>> GetRoster()
    {
        var result = await _mediator.Send(new GetEscalationRosterQuery());
        return Ok(result);
    }

    [HttpPut("Reorder")]
    public async Task<ActionResult<HttpResultResource>> Reorder([FromBody] ReorderEscalationRosterCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}
