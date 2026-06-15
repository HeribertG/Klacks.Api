// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Admin-only REST API for the emergent skill-relationship graph: lists what Klacksy has noticed and
/// lets an admin accept or dismiss an insight, which feeds back as a strong learning signal.
/// </summary>
/// <param name="mediator">Dispatches the skill-relation query and accept/dismiss commands.</param>

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
[Route("api/backend/assistant/skill-relations")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
public class SkillRelationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SkillRelationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<List<SkillRelationDto>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetSkillRelationsQuery(), cancellationToken));
    }

    [HttpPost("{id:guid}/accept")]
    public async Task<ActionResult<SkillRelationDto>> Accept(Guid id, CancellationToken cancellationToken)
    {
        var dto = await _mediator.Send(new AcceptSkillRelationCommand(id), cancellationToken);
        return dto == null ? NotFound() : Ok(dto);
    }

    [HttpPost("{id:guid}/dismiss")]
    public async Task<ActionResult<SkillRelationDto>> Dismiss(Guid id, CancellationToken cancellationToken)
    {
        var dto = await _mediator.Send(new DismissSkillRelationCommand(id), cancellationToken);
        return dto == null ? NotFound() : Ok(dto);
    }
}
