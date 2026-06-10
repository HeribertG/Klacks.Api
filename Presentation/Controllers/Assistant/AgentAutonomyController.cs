// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// REST API for the per-user autonomy level. Lets the Settings UI read and change how much
/// Klacksy may execute without explicit confirmation (0 propose-only to 3 fully autonomous).
/// </summary>
/// <param name="mediator">Dispatches the autonomy level query and command.</param>

using System.Security.Claims;
using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.Assistant;

[ApiController]
[Route("api/backend/assistant/autonomy-level")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class AgentAutonomyController : ControllerBase
{
    private readonly IMediator _mediator;

    public AgentAutonomyController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<AutonomyLevelDto>> GetMyLevel(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        var level = await _mediator.Send(new GetAutonomyLevelQuery(userId), cancellationToken);
        return Ok(ToDto(level));
    }

    [HttpPut]
    public async Task<ActionResult<AutonomyLevelDto>> SetMyLevel(
        [FromBody] UpdateAutonomyLevelRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        if (request.Level < (int)AutonomyDefaults.MinimumLevel || request.Level > (int)AutonomyDefaults.MaximumLevel)
        {
            return BadRequest(
                $"Level must be between {(int)AutonomyDefaults.MinimumLevel} and {(int)AutonomyDefaults.MaximumLevel}.");
        }

        var saved = await _mediator.Send(
            new SetAutonomyLevelCommand(userId, (AutonomyLevel)request.Level), cancellationToken);
        return Ok(ToDto(saved));
    }

    private static AutonomyLevelDto ToDto(AutonomyLevel level)
        => new() { Level = (int)level, Name = level.ToString() };

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }

        return Guid.Empty;
    }
}
