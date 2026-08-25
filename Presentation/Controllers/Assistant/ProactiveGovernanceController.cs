// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// REST API behind the proactive-governance settings card: reads how far Klacksy may act by itself per
/// trigger kind and writes a single rule or the global kill switch. Admin-only, because this decides
/// what the assistant is allowed to do to the schedule without being asked.
/// </summary>
/// <param name="mediator">Dispatches the governance query and command.</param>

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
[Route("api/backend/assistant/proactive-governance")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
public class ProactiveGovernanceController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProactiveGovernanceController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<ProactiveGovernanceDto>> Get(CancellationToken cancellationToken)
    {
        var governance = await _mediator.Send(new GetProactiveGovernanceQuery(), cancellationToken);
        return Ok(governance);
    }

    [HttpPut]
    public async Task<ActionResult<ProactiveGovernanceDto>> Put(
        [FromBody] UpdateProactiveGovernanceRequest request,
        CancellationToken cancellationToken)
    {
        ProactiveMaxAction? maxAction = null;
        if (request.MaxAction is int rawMaxAction)
        {
            if (!Enum.IsDefined(typeof(ProactiveMaxAction), rawMaxAction))
            {
                return BadRequest($"Unknown maxAction value '{rawMaxAction}'.");
            }

            maxAction = (ProactiveMaxAction)rawMaxAction;
        }

        var command = new SetProactiveGovernanceCommand(
            TriggerKind: request.TriggerKind,
            GroupId: request.GroupId,
            MaxAction: maxAction,
            Enabled: request.Enabled,
            ResponsibleOwnerUserId: request.ResponsibleOwnerUserId,
            ClearResponsibleOwner: request.ClearResponsibleOwner,
            DailyActionBudget: request.DailyActionBudget,
            WindowActionLimit: request.WindowActionLimit,
            WindowMinutes: request.WindowMinutes,
            KillSwitch: request.KillSwitch);

        var governance = await _mediator.Send(command, cancellationToken);
        return Ok(governance);
    }
}
