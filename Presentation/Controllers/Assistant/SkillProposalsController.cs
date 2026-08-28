// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// What is left of the description-proposal workflow once the learning loop owns it: an administrator's
/// override of the routing regression gate. The loop generates the proposals, applies the ones that leave
/// every golden case routing, and blocks the rest; only the blocked ones are still a human decision, and
/// rejecting one is the way to say the wording is wrong rather than merely risky.
/// Listing them is not here any more either - the "Klacksy learned" card shows them next to the learned
/// phrases, because an administrator judges both the same way.
/// The JWT scheme is pinned explicitly: AddIdentity overrides the runtime default to cookie
/// authentication, so a bare role gate would answer 401 to every JWT caller.
/// </summary>
/// <param name="mediator">Dispatches the approve and reject commands</param>

using System.Security.Claims;
using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.Assistant;

[ApiController]
[Route("api/backend/assistant/skill-proposals")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
public class SkillProposalsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SkillProposalsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ApproveProposedSkillChangeResult>> Approve(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var reviewedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(reviewedBy))
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new ApproveProposedSkillChangeCommand
        {
            ProposalId = id,
            ReviewedBy = reviewedBy
        }, cancellationToken);

        if (!result.Applied)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result);
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<RejectProposedSkillChangeResult>> Reject(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var reviewedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(reviewedBy))
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new RejectProposedSkillChangeCommand
        {
            ProposalId = id,
            ReviewedBy = reviewedBy
        }, cancellationToken);

        if (!result.Rejected)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result);
    }
}
