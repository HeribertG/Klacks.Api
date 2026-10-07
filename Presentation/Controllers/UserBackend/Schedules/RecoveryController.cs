// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Commands.Schedules;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Queries.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Schedules;

/// <summary>
/// REST entry point for the reactive recovery flow. CoverAbsence records the absence (Break) and a
/// rule-compliant replacement per slot as an isolated, propose-only AnalyseScenario for human review;
/// it never accepts the scenario. Candidates lists the ranked alternatives for one slot so the planner can
/// pick a different person than the engine's first choice. Open to every authenticated user (owner
/// decision 2026-10-06): a sick call is handled by whoever plans the group, the flow only proposes a
/// scenario, accepting one is open to every authenticated user anyway (AnalyseScenariosController), the
/// compliance override is authorised separately by ISupervisorOverrideAuthorizer, and group visibility is
/// enforced inside the handlers: CoverAbsence refuses an absent employee outside the caller's visibility like
/// a missing one and drops repair options touching hidden employees; Candidates answers a group outside the
/// caller's visibility exactly like an unknown group (empty lists) and limits both the eligible and the
/// excluded list to employees the caller may see.
/// </summary>
/// <param name="mediator">Dispatches the reused CoverAbsenceCommand and FindReplacementQuery.</param>
[ApiController]
[Route("api/backend/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
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
                    request.UntilDate, request.OverrideBlock, request.Language, request.NotifyEscalationRoster,
                    request.ReportedAtUtc, ReplacementRequestSource.PlannerDialog),
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
    /// <param name="groupId">Group whose visible members form the candidate pool; a hidden group yields empty lists</param>
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
            new GetReplacementCandidatesQuery(
                new FindReplacementQuery(shiftId, date, startTime, endTime, groupId, analyseToken, overrideBlock)), ct);

        return Ok(result);
    }

    /// <summary>
    /// Entries of the replacement request book (who was proposed or asked for which slot, with the answer and the
    /// short-notice verdict). Only rows whose candidate the caller may see are returned.
    /// </summary>
    /// <param name="absentClientId">Optional absent employee</param>
    /// <param name="fromDate">Optional first slot date (inclusive)</param>
    /// <param name="untilDate">Optional last slot date (inclusive)</param>
    /// <param name="analyseToken">Optional scenario token</param>
    [HttpGet("Requests")]
    public async Task<ActionResult<IReadOnlyList<ReplacementRequestResource>>> Requests(
        [FromQuery] Guid? absentClientId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? untilDate,
        [FromQuery] Guid? analyseToken,
        CancellationToken ct)
    {
        var rows = await _mediator.Send(
            new ListReplacementRequestsQuery(absentClientId, fromDate, untilDate, analyseToken), ct);

        return Ok(rows);
    }

    /// <summary>
    /// Records the candidate's answer (Requested, Accepted, Declined, NotReached) on an existing request row. A
    /// missing row and a row with a hidden candidate both answer 404.
    /// </summary>
    /// <param name="id">Row id</param>
    /// <param name="request">The answer</param>
    [HttpPut("Requests/{id:guid}/Outcome")]
    public async Task<ActionResult<ReplacementRequestResource>> SetOutcome(
        Guid id,
        [FromBody] SetReplacementOutcomeRequest request,
        CancellationToken ct)
    {
        var row = await _mediator.Send(new SetReplacementRequestOutcomeCommand(id, request.Outcome), ct);

        return Ok(row);
    }

    /// <summary>
    /// Records a contact attempt with an alternative candidate for one slot (upsert on scenario token, candidate,
    /// shift and date). Only the fact is stored; the proposal in the scenario stays unchanged.
    /// </summary>
    /// <param name="request">Slot, candidate, context and the answer</param>
    [HttpPost("Requests")]
    public async Task<ActionResult<ReplacementRequestResource>> RecordContact(
        [FromBody] RecordReplacementContactRequest request,
        CancellationToken ct)
    {
        var row = await _mediator.Send(new RecordReplacementContactCommand(request), ct);

        return Ok(row);
    }
}
