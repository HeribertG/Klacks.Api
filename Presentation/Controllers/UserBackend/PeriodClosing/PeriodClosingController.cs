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
/// Admin-only endpoints for sealing and unsealing work periods and reviewing audit trails.
/// </summary>
[Authorize(Roles = Roles.Admin)]
public class PeriodClosingController : BaseController
{
    private readonly IMediator _mediator;

    public PeriodClosingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("Seal")]
    public async Task<ActionResult<int>> Seal([FromBody] ClosePeriodByGroupCommand command)
    {
        var affected = await _mediator.Send(command);
        return Ok(affected);
    }

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

    [HttpGet("UsedPeriods")]
    public async Task<ActionResult<List<UsedPeriodDto>>> GetUsedPeriods()
    {
        var result = await _mediator.Send(new GetUsedPeriodsQuery());
        return Ok(result);
    }

    [HttpGet("AuditLog")]
    public async Task<ActionResult<List<PeriodAuditLogDto>>> GetAuditLog(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to)
    {
        var result = await _mediator.Send(new GetPeriodAuditLogQuery(from, to));
        return Ok(result);
    }

    [HttpGet("ExportLog")]
    public async Task<ActionResult<List<ExportLogDto>>> GetExportLog(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to)
    {
        var result = await _mediator.Send(new GetExportLogQuery(from, to));
        return Ok(result);
    }
}
