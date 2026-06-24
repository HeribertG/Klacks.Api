// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Schedules;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Schedules;

/// <summary>
/// REST entry point for the reactive recovery flow. CoverAbsence records the absence (Break) and a
/// rule-compliant replacement per slot as an isolated, propose-only AnalyseScenario for human review;
/// it never accepts the scenario. Admin role is required because the flow creates a schedule scenario.
/// </summary>
/// <param name="mediator">Dispatches the reused CoverAbsenceCommand to the deterministic recovery engine.</param>
[ApiController]
[Route("api/backend/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
public sealed class RecoveryController : ControllerBase
{
    private readonly IMediator _mediator;

    public RecoveryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("CoverAbsence")]
    public async Task<ActionResult<CoverAbsenceOutcome>> CoverAbsence(
        [FromBody] CoverAbsenceRequest request,
        CancellationToken ct)
    {
        var outcome = await _mediator.Send(
            new CoverAbsenceCommand(request.ClientId, request.Date, request.GroupId, request.AbsenceId), ct);

        return Ok(outcome);
    }
}
