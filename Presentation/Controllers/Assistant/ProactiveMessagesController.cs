// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// REST API for the proactive assistant message inbox: lists a user's own messages, exposes the
/// unread count for the assistant badge, marks a single message, a listed page of messages or all
/// messages as read and stores the user's reaction (helpful / dismissed), keyed by the message id
/// the notification hub sent. A dismissal may carry a reject reason, which is written back onto the
/// condition-ledger finding the message reported; sending one without a dismissal is a client error.
/// </summary>
/// <param name="mediator">Dispatches the inbox queries and commands.</param>

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
[Route("api/backend/assistant/proactive-messages")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ProactiveMessagesController : ControllerBase
{
    private const string InvalidReactionMessage = "Invalid reaction. Allowed values: helpful, dismissed.";

    private const string InvalidRejectReasonMessage =
        "Invalid reject reason. Allowed values: generallyUnwanted, wrongThisTime, alreadyHandled, noReason.";

    private const string RejectReasonWithoutDismissMessage =
        "A reject reason is only allowed together with the dismissed reaction.";

    private const string TooManyIdsMessage = "Too many message ids in one request.";

    private readonly IMediator _mediator;

    public ProactiveMessagesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProactiveInboxMessageDto>>> GetMessages(
        [FromQuery] bool unreadOnly,
        [FromQuery] int? take,
        CancellationToken cancellationToken)
    {
        var messages = await _mediator.Send(new GetProactiveMessagesQuery
        {
            UserId = GetCurrentUserId(),
            UnreadOnly = unreadOnly,
            Take = take
        }, cancellationToken);

        return Ok(messages);
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<ProactiveUnreadCountDto>> GetUnreadCount(CancellationToken cancellationToken)
    {
        var count = await _mediator.Send(new GetProactiveUnreadCountQuery
        {
            UserId = GetCurrentUserId()
        }, cancellationToken);

        return Ok(new ProactiveUnreadCountDto { Count = count });
    }

    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        var found = await _mediator.Send(new MarkProactiveMessageReadCommand
        {
            Id = id,
            UserId = GetCurrentUserId()
        }, cancellationToken);

        return found ? NoContent() : NotFound();
    }

    [HttpPut("read")]
    public async Task<IActionResult> MarkRead([FromBody] MarkProactiveMessagesReadRequest request, CancellationToken cancellationToken)
    {
        if (request.Ids.Count > ProactiveInboxDefaults.MaxListTake)
        {
            return BadRequest(TooManyIdsMessage);
        }

        await _mediator.Send(new MarkProactiveMessagesReadCommand
        {
            Ids = request.Ids,
            UserId = GetCurrentUserId()
        }, cancellationToken);

        return NoContent();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        await _mediator.Send(new MarkAllProactiveMessagesReadCommand
        {
            UserId = GetCurrentUserId()
        }, cancellationToken);

        return NoContent();
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

        AgentConditionRejectReason? rejectReason = null;
        if (!string.IsNullOrWhiteSpace(request.RejectReason))
        {
            if (reaction != ProactiveReaction.Dismissed)
            {
                return BadRequest(RejectReasonWithoutDismissMessage);
            }

            if (!Enum.TryParse<AgentConditionRejectReason>(request.RejectReason, ignoreCase: true, out var parsedReason)
                || !Enum.IsDefined(parsedReason))
            {
                return BadRequest(InvalidRejectReasonMessage);
            }

            rejectReason = parsedReason;
        }

        var found = await _mediator.Send(new SetProactiveReactionCommand
        {
            Id = id,
            UserId = GetCurrentUserId(),
            Reaction = reaction,
            RejectReason = rejectReason
        }, cancellationToken);

        return found ? NoContent() : NotFound();
    }

    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }
}
