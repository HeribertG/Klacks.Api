// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// REST API of the UI explain offers (T5): whether the UI may offer Klacksy's short explanation of a topic to the
/// signed-in user, and the user's reaction to that offer. Every call acts on the caller's own state only - the user id
/// is always taken from the token, never from the request. Open to everyone who may use the assistant (planner floor).
/// A token without a user account gets 400, not 401: a 401 makes the SPA log the user out.
/// </summary>
/// <param name="mediator">Runs the state query and the reaction command</param>

using System.Security.Claims;
using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.Assistant;

[ApiController]
[Route("api/backend/assistant/explain-offers")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = AuthorizationPolicies.RequireAssistant)]
public class UiExplainOffersController : ControllerBase
{
    private const string UnknownTopicMessage = "Unknown explain offer topic";
    private const string UnknownReactionMessage = "Unknown explain offer topic or outcome";
    private const string NoUserMessage = "The token carries no user account";

    private readonly IMediator _mediator;

    public UiExplainOffersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{topicKey}")]
    public async Task<ActionResult<UiExplainOfferStateResource>> GetState(string topicKey)
    {
        var userId = CurrentUserId();
        if (userId == null)
        {
            return BadRequest(NoUserMessage);
        }

        var state = await _mediator.Send(
            new GetUiExplainOfferStateQuery { UserId = userId, TopicKey = topicKey },
            HttpContext.RequestAborted);
        return state == null ? BadRequest(UnknownTopicMessage) : Ok(state);
    }

    [HttpPost("{topicKey}/reaction")]
    public async Task<IActionResult> RecordReaction(string topicKey, [FromBody] UiExplainOfferReactionRequest request)
    {
        var userId = CurrentUserId();
        if (userId == null)
        {
            return BadRequest(NoUserMessage);
        }

        var recorded = await _mediator.Send(
            new RecordUiExplainOfferReactionCommand { UserId = userId, TopicKey = topicKey, Outcome = request.Outcome },
            HttpContext.RequestAborted);
        return recorded ? NoContent() : BadRequest(UnknownReactionMessage);
    }

    private string? CurrentUserId()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out _) ? value : null;
    }
}
