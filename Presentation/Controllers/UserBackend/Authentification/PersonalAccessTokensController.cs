// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Manages the personal access tokens of the authenticated user. Deliberately restricted to
/// the JWT bearer scheme so that a personal access token can never manage tokens itself.
/// </summary>

using System.Security.Claims;
using Klacks.Api.Application.Commands.Authentification;
using Klacks.Api.Application.DTOs.Authentification;
using Klacks.Api.Application.Queries.Authentification;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Authentification;

[ApiController]
[Route("api/backend/personal-access-tokens")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class PersonalAccessTokensController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PersonalAccessTokensController> _logger;

    public PersonalAccessTokensController(IMediator mediator, ILogger<PersonalAccessTokensController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<PersonalAccessTokenCreatedDto>> Create([FromBody] CreatePersonalAccessTokenRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var created = await _mediator.Send(new CreatePersonalAccessTokenCommand(userId, request.Name, request.ExpiresInDays));

        _logger.LogInformation("Personal access token {TokenId} created", created.Id);

        return Ok(created);
    }

    [HttpGet]
    public async Task<ActionResult<List<PersonalAccessTokenListItemDto>>> GetOwnTokens()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var tokens = await _mediator.Send(new GetPersonalAccessTokensQuery(userId));

        return Ok(tokens);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Revoke(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var revoked = await _mediator.Send(new RevokePersonalAccessTokenCommand(id, userId));
        if (!revoked)
        {
            return NotFound();
        }

        _logger.LogInformation("Personal access token {TokenId} revoked", id);

        return NoContent();
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
