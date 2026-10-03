// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Admin-only REST surface for planning constraints: CRUD plus the decisions of the UI pending list
/// (approve, reject) and revocation. Approval exists ONLY here (owner decision 2026-10-03). What keeps it there
/// is this class itself: no skill sends the approve command, the controller carries no ICrudResourceController
/// marker (SelfApiRouteResolver never hands its route to a skill), and it accepts only an Admin JWT - pinned
/// explicitly, because AddIdentity makes cookie auth the default. The MCP role cap is an extra layer, not the
/// protection this relies on.
/// </summary>
/// <param name="mediator">Dispatches the planning-constraint commands and queries</param>

using System.Security.Claims;
using Klacks.Api.Application.Commands.PlanningConstraints;
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.Queries.PlanningConstraints;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Scheduling;

[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
public class PlanningConstraintsController : BaseController
{
    private const string ApproveRoute = "{id:guid}/approve";
    private const string RejectRoute = "{id:guid}/reject";
    private const string RevokeRoute = "{id:guid}/revoke";
    private const string IdRoute = "{id:guid}";

    private readonly IMediator _mediator;

    public PlanningConstraintsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PlanningConstraintResource>>> GetAll([FromQuery] RuleApprovalStatus? status)
    {
        return Ok(await _mediator.Send(new ListPlanningConstraintsQuery(status)));
    }

    [HttpGet(IdRoute)]
    public async Task<ActionResult<PlanningConstraintResource>> Get([FromRoute] Guid id)
    {
        var resource = await _mediator.Send(new GetPlanningConstraintQuery(id));
        return resource is null ? NotFound() : Ok(resource);
    }

    [HttpPost]
    public async Task<ActionResult<PlanningConstraintResource>> Post([FromBody] PlanningConstraintWriteResource resource)
    {
        var actor = CurrentActor();
        if (actor is null)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        return Ok(await _mediator.Send(new CreatePlanningConstraintCommand(resource, actor)));
    }

    [HttpPut(IdRoute)]
    public async Task<ActionResult<PlanningConstraintResource>> Put([FromRoute] Guid id, [FromBody] PlanningConstraintWriteResource resource)
    {
        var actor = CurrentActor();
        if (actor is null)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        return Ok(await _mediator.Send(new UpdatePlanningConstraintCommand(id, resource, actor)));
    }

    [HttpPost(ApproveRoute)]
    public async Task<ActionResult<PlanningConstraintResource>> Approve([FromRoute] Guid id)
    {
        var actor = CurrentActor();
        if (actor is null)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        return Ok(await _mediator.Send(new ApprovePlanningConstraintCommand(id, actor)));
    }

    [HttpPost(RejectRoute)]
    public async Task<ActionResult<PlanningConstraintResource>> Reject([FromRoute] Guid id)
    {
        return Ok(await _mediator.Send(new RejectPlanningConstraintCommand(id)));
    }

    [HttpPost(RevokeRoute)]
    public async Task<ActionResult<PlanningConstraintResource>> Revoke([FromRoute] Guid id)
    {
        return Ok(await _mediator.Send(new RevokePlanningConstraintCommand(id)));
    }

    [HttpDelete(IdRoute)]
    public async Task<ActionResult<PlanningConstraintResource>> Delete([FromRoute] Guid id)
    {
        return Ok(await _mediator.Send(new DeletePlanningConstraintCommand(id)));
    }

    private string? CurrentActor()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return string.IsNullOrWhiteSpace(userId) ? null : userId;
    }
}
