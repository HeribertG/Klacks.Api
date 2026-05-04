// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Endpoints for reading and saving the per-user, per-group client sort order.
/// UserId is always resolved from the JWT claim — never from the request body.
/// </summary>

using System.Security.Claims;
using Klacks.Api.Application.Commands.Schedule;
using Klacks.Api.Application.DTOs;
using Klacks.Api.Application.Queries.Schedule;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Schedule;

[ApiController]
public class ClientSortPreferencesController : BaseController
{
    private readonly IMediator _mediator;
    private readonly ILogger<ClientSortPreferencesController> _logger;

    public ClientSortPreferencesController(
        IMediator mediator,
        ILogger<ClientSortPreferencesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet("{groupId:guid}")]
    public async Task<ActionResult<List<ClientSortOrderDto>>> GetSortOrder(Guid groupId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _mediator.Send(new GetClientSortOrderQuery(userId, groupId));
        return Ok(result);
    }

    [HttpPut("{groupId:guid}")]
    public async Task<ActionResult> SaveSortOrder(
        Guid groupId, [FromBody] List<ClientSortOrderDto> entries)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        await _mediator.Send(new SaveClientSortOrderCommand(userId, groupId, entries));
        return NoContent();
    }
}
