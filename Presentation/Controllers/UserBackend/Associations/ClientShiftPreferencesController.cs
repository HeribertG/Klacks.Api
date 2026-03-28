// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// API controller for managing client shift preferences (whitelist, preferred, blacklist).
/// </summary>
/// <param name="clientId">The client whose preferences to query or update</param>
using Klacks.Api.Application.Commands.ClientShiftPreferences;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Application.Queries.ClientShiftPreferences;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Associations;

public class ClientShiftPreferencesController : BaseController
{
    private readonly IMediator _mediator;

    public ClientShiftPreferencesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<List<ClientShiftPreferenceResource>>> GetByClient([FromQuery] Guid clientId)
    {
        var result = await _mediator.Send(new ListByClientQuery(clientId));
        return Ok(result);
    }

    [HttpGet("available-shifts")]
    public async Task<ActionResult<List<AvailableShiftResource>>> GetAvailableShifts([FromQuery] Guid clientId)
    {
        var result = await _mediator.Send(new GetAvailableShiftsQuery(clientId));
        return Ok(result);
    }

    [HttpPost("bulk")]
    public async Task<ActionResult<List<ClientShiftPreferenceResource>>> SaveAll(
        [FromBody] SaveClientShiftPreferencesCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}
