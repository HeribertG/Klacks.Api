// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Commands.Schedules;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Queries.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Schedules;

/// <summary>
/// REST entry point for the reactive recovery flow. CoverAbsence records the absence (Break) and a
/// rule-compliant replacement per slot as an isolated, propose-only AnalyseScenario for human review;
/// it never accepts the scenario. Candidates lists the ranked alternatives for one slot so the planner can
/// pick a different person than the engine's first choice. Admins and supervisors (Authorised) may use
/// both: a sick call is handled by the planner on duty, not only by an administrator, and accepting the
/// resulting scenario is open to every authenticated user anyway (AnalyseScenariosController).
/// </summary>
/// <param name="mediator">Dispatches the reused CoverAbsenceCommand and FindReplacementQuery.</param>
[ApiController]
[Route("api/backend/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{Roles.Admin},{Roles.Authorised}")]
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
        try
        {
            var outcome = await _mediator.Send(
                new CoverAbsenceCommand(
                    request.ClientId, request.Date, request.GroupId, request.AbsenceId,
                    request.UntilDate, request.OverrideBlock, request.Language, request.NotifyEscalationRoster),
                ct);

            return Ok(outcome);
        }
        catch (ArgumentException ex)
        {
            // The handler rejects an inverted or over-long period; without this it would surface as a 500.
            return BadRequest(new ProblemDetails { Title = "Bad Request", Detail = ex.Message });
        }
    }

    /// <summary>
    /// Ranked replacement candidates for one slot (eligible best-first, excluded with the reason), the same
    /// search the find_replacement skill runs. Read-only: lets the planner see who else could take the shift
    /// and why the others cannot, instead of accepting or rejecting a single engine proposal blind.
    /// </summary>
    /// <param name="shiftId">Shift to fill</param>
    /// <param name="date">Workday</param>
    /// <param name="startTime">Slot start</param>
    /// <param name="endTime">Slot end</param>
    /// <param name="groupId">Group whose members form the candidate pool</param>
    /// <param name="analyseToken">Optional scenario token; candidates are checked against the isolated scenario</param>
    /// <param name="overrideBlock">K1 supervisor override for a Block-mode compliance exclusion</param>
    [HttpGet("Candidates")]
    public async Task<ActionResult<ReplacementSearchResult>> Candidates(
        [FromQuery] Guid shiftId,
        [FromQuery] DateOnly date,
        [FromQuery] TimeOnly startTime,
        [FromQuery] TimeOnly endTime,
        [FromQuery] Guid groupId,
        [FromQuery] Guid? analyseToken,
        [FromQuery] bool overrideBlock,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new FindReplacementQuery(shiftId, date, startTime, endTime, groupId, analyseToken, overrideBlock), ct);

        return Ok(result);
    }
}
