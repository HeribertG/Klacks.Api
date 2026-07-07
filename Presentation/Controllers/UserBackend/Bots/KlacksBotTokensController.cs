// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Manages Klacks bot tokens (read-only credentials for the external Otto bot). Admin-only:
/// minting or revoking one of these tokens is a security-sensitive action, same as personal
/// access tokens and ERP import tokens.
/// </summary>
using Klacks.Api.Application.Commands.Bots;
using Klacks.Api.Application.DTOs.Bots;
using Klacks.Api.Application.Queries.Bots;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Bots;

[ApiController]
[Route("api/backend/bot-tokens")]
[Authorize(Roles = Roles.Admin)]
public class KlacksBotTokensController : BaseController
{
    private readonly IMediator _mediator;
    private readonly ILogger<KlacksBotTokensController> _logger;

    public KlacksBotTokensController(IMediator mediator, ILogger<KlacksBotTokensController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<KlacksBotTokenListItemDto>>> Get()
    {
        var tokens = await _mediator.Send(new GetKlacksBotTokensQuery());
        return Ok(tokens);
    }

    [HttpPost]
    public async Task<ActionResult<KlacksBotTokenCreatedDto>> Create([FromBody] CreateKlacksBotTokenRequest request)
    {
        var created = await _mediator.Send(new CreateKlacksBotTokenCommand(request.Name, request.ExpiresInDays));

        _logger.LogInformation("Klacks bot token {TokenId} created", created.Id);

        return Ok(created);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Revoke(Guid id)
    {
        var revoked = await _mediator.Send(new RevokeKlacksBotTokenCommand(id));
        if (!revoked)
        {
            return NotFound();
        }

        _logger.LogInformation("Klacks bot token {TokenId} revoked", id);

        return NoContent();
    }
}

public record CreateKlacksBotTokenRequest(string Name, int? ExpiresInDays);
