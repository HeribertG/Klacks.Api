// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// REST API for reacting to proactive assistant messages: the chat UI marks a delivered proactive
/// message as helpful or dismissed, keyed by the message id the notification hub sent.
/// </summary>
/// <param name="mediator">Dispatches the set-reaction command.</param>

using System.Security.Claims;
using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.Assistant;

[ApiController]
[Route("api/backend/assistant/proactive-messages")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ProactiveMessagesController : ControllerBase
{
    private const string InvalidReactionMessage = "Invalid reaction. Allowed values: helpful, dismissed.";

    private readonly IMediator _mediator;

    public ProactiveMessagesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPut("{id:guid}/reaction")]
    public async Task<IActionResult> SetReaction(Guid id, [FromBody] SetProactiveReactionRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ProactiveReaction>(request.Reaction, ignoreCase: true, out var reaction)
            || !Enum.IsDefined(reaction)
            || reaction == ProactiveReaction.None)
        {
            return BadRequest(InvalidReactionMessage);
        }

        var found = await _mediator.Send(new SetProactiveReactionCommand
        {
            Id = id,
            UserId = GetCurrentUserId(),
            Reaction = reaction
        }, cancellationToken);

        return found ? NoContent() : NotFound();
    }

    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }
}
