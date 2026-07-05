// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.PeriodClosing;
using Klacks.Api.Application.DTOs.PeriodClosing;
using Klacks.Api.Application.Queries.PeriodClosing;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.PeriodClosing;

/// <summary>
/// Endpoints for sealing and unsealing work periods and reviewing audit trails.
/// Sealing/unsealing and audit/export views are Admin-only; reading sealed periods
/// is open to all authenticated users since the schedule UI needs it for every user.
/// </summary>
public class PeriodClosingController : BaseController
{
    private readonly IMediator _mediator;

    public PeriodClosingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost("Seal")]
    public async Task<ActionResult<int>> Seal([FromBody] ClosePeriodByGroupCommand command)
    {
        var affected = await _mediator.Send(command);
        return Ok(affected);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost("Unseal")]
    public async Task<ActionResult<int>> Unseal([FromBody] ReopenPeriodByGroupCommand command)
    {
        var affected = await _mediator.Send(command);
        return Ok(affected);
    }

    [HttpGet("SealedPeriods")]
    public async Task<ActionResult<List<SealedPeriodSummaryDto>>> GetSealedPeriods(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] Guid? groupId)
    {
        var result = await _mediator.Send(new GetSealedPeriodsQuery(from, to, groupId));
        return Ok(result);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpGet("UsedPeriods")]
    public async Task<ActionResult<List<UsedPeriodDto>>> GetUsedPeriods()
    {
        var result = await _mediator.Send(new GetUsedPeriodsQuery());
        return Ok(result);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpGet("Issues")]
    public async Task<ActionResult<List<PeriodIssueDto>>> GetIssues(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] Guid? groupId)
    {
        var result = await _mediator.Send(new GetPeriodIssuesQuery(from, to, groupId));
        return Ok(result);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpGet("AuditLog")]
    public async Task<ActionResult<List<PeriodAuditLogDto>>> GetAuditLog(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to)
    {
        var result = await _mediator.Send(new GetPeriodAuditLogQuery(from, to));
        return Ok(result);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpGet("ExportLog")]
    public async Task<ActionResult<List<ExportLogDto>>> GetExportLog(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to)
    {
        var result = await _mediator.Send(new GetExportLogQuery(from, to));
        return Ok(result);
    }
}
