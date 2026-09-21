// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// REST API behind the standing-approval card: lists the advance approvals, grants one and revokes one.
/// Admin-only and scheme-pinned, for two reasons that both matter. It decides whether Klacksy may change
/// the schedule without a human being asked per finding, which is the same decision
/// ProactiveGovernanceController guards; and the granting account is the identity every execution under
/// the grant borrows, so the grant is only ever taken from the authenticated principal, never from the
/// request body.
///
/// A collision with a still-running grant answers 409, not 400: the request was well formed and the
/// remedy is a different action (revoke the running grant), which is exactly what a conflict means.
/// </summary>
/// <param name="mediator">Dispatches the standing-approval query and commands.</param>

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
[Route("api/backend/assistant/proactive-standing-approvals")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
public class ProactiveStandingApprovalsController : ControllerBase
{
    private const string UnknownUserMessage = "The authenticated account could not be identified.";

    private readonly IMediator _mediator;

    public ProactiveStandingApprovalsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StandingApprovalDto>>> Get(CancellationToken cancellationToken)
    {
        var approvals = await _mediator.Send(new GetStandingApprovalsQuery(), cancellationToken);
        return Ok(approvals);
    }

    [HttpPost]
    public async Task<ActionResult<StandingApprovalDto>> Post(
        [FromBody] GrantStandingApprovalRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId() is not Guid grantedByUserId)
        {
            return BadRequest(UnknownUserMessage);
        }

        var result = await _mediator.Send(
            new GrantStandingApprovalCommand(
                TriggerKind: request.TriggerKind,
                GroupId: request.GroupId,
                DurationDays: request.DurationDays,
                DailyBudget: request.DailyBudget,
                GrantedByUserId: grantedByUserId),
            cancellationToken);

        return result.Outcome switch
        {
            GrantStandingApprovalOutcome.Granted => Ok(result.Approval),
            GrantStandingApprovalOutcome.AlreadyActive => Conflict(result.Reason),
            _ => BadRequest(result.Reason)
        };
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (CurrentUserId() is not Guid revokedByUserId)
        {
            return BadRequest(UnknownUserMessage);
        }

        var revoked = await _mediator.Send(
            new RevokeStandingApprovalCommand(id, revokedByUserId), cancellationToken);

        return revoked ? NoContent() : NotFound();
    }

    private Guid? CurrentUserId() =>
        Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) ? userId : null;
}
