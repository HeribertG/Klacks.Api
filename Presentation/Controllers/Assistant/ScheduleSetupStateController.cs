// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// REST API exposing the installation-wide setup snapshot (order -> shift -> assignment chain) that
/// the frontend needs to tell "this installation is empty" apart from "the order list is filtered to
/// nothing right now". Same audience as the get_setup_guidance skill: planners holding CanViewShifts.
/// </summary>
/// <param name="mediator">Dispatches the setup-state query.</param>

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.Assistant;

[ApiController]
[Route("api/backend/assistant/setup-state")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ScheduleSetupStateController : ControllerBase
{
    private readonly IMediator _mediator;

    public ScheduleSetupStateController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<ScheduleSetupStateResource>> Get(CancellationToken cancellationToken)
    {
        if (!Permissions.HasPermission(User.GetUserRights(), Permissions.CanViewShifts))
        {
            return Forbid(JwtBearerDefaults.AuthenticationScheme);
        }

        var result = await _mediator.Send(new GetScheduleSetupStateQuery(), cancellationToken);
        return Ok(result);
    }
}
