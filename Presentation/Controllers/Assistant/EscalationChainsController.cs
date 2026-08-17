// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Admin REST API for the escalation intervention list: lists every Running chain (who was woken,
/// who acknowledged, the remaining deadline) and lets the requesting user take one over or cancel it
/// with a mandatory reason (Owner decision B7).
/// </summary>
/// <param name="mediator">Dispatches the chain list query and the acknowledge/cancel commands.</param>

using System.Security.Claims;
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
[Route("api/backend/assistant/escalation-chains")]
[Authorize(Roles = Roles.Admin)]
public class EscalationChainsController : ControllerBase
{
    private readonly IMediator _mediator;

    public EscalationChainsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EscalationChainSummaryResource>>> GetRunning()
    {
        var result = await _mediator.Send(new GetRunningEscalationChainsQuery(CurrentUserId()));
        return Ok(result);
    }

    [HttpPut("{id:guid}/acknowledge")]
    public async Task<ActionResult<HttpResultResource>> Acknowledge(Guid id)
    {
        var result = await _mediator.Send(new AcknowledgeEscalationChainCommand(id, CurrentUserId()));
        return Ok(result);
    }

    [HttpPut("{id:guid}/cancel")]
    public async Task<ActionResult<HttpResultResource>> Cancel(Guid id, [FromBody] CancelEscalationChainResource resource)
    {
        var result = await _mediator.Send(
            new CancelEscalationChainCommand(id, CurrentUserId(), CurrentUserName(), resource.Reason));
        return Ok(result);
    }

    private string CurrentUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

    private string CurrentUserName() => User.FindFirst(ClaimTypes.Name)?.Value ?? CurrentUserId();
}
