// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Admin REST API for a group's escalation call list: lists the current roster (re-derived from
/// GroupVisibility, admin overrides preserved) and lets an admin set a manual order.
/// </summary>
/// <param name="mediator">Dispatches the roster query and the reorder command.</param>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.DTOs;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.Assistant;

[ApiController]
[Route("api/backend/assistant/escalation-roster")]
[Authorize(Roles = Roles.Admin)]
public class EscalationRosterController : ControllerBase
{
    private readonly IMediator _mediator;

    public EscalationRosterController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EscalationRosterEntryResource>>> GetRoster([FromQuery] Guid groupId)
    {
        var result = await _mediator.Send(new GetEscalationRosterQuery(groupId));
        return Ok(result);
    }

    [HttpPut]
    public async Task<ActionResult<HttpResultResource>> Reorder([FromBody] ReorderEscalationRosterCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}
