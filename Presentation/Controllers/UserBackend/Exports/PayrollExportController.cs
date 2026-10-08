// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Controller for the person-based payroll (country-pack) export of a closed period. Not group-scoped: every person
/// with closed entries appears once, whatever groups they belong to. Restricted to admins: the file holds the
/// wages-relevant time values of the persons. Preview answers what an export would do (blockers, new or changed
/// persons) without writing; the export itself returns the file of the new or changed persons (or of the selected
/// persons for a supplementary export) and is answered with 409 and a machine-readable code when the period is
/// blocked, nothing is new or another export was faster. Every run is stored and can be downloaded again through
/// its export log id. Entries the formatter could not write are reported in the ExportResponseHeaders.SkippedEntries
/// header (an unparsable absence mapping in AbsenceMappingInvalid); the number of exported persons and whether the
/// file is a supplementary export are reported in ExportResponseHeaders.Persons and Supplementary.
/// </summary>
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Exports;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Application.Queries.Exports;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Exports;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
public class PayrollExportController : BaseController
{
    private readonly IMediator _mediator;

    public PayrollExportController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("Preview")]
    public async Task<ActionResult<PayrollExportPreviewDto>> Preview(
        [FromQuery] DateOnly fromDate,
        [FromQuery] DateOnly untilDate,
        [FromQuery] string format,
        [FromQuery] List<Guid>? clientIds,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Guid>? scope = clientIds is { Count: > 0 } ? clientIds : null;
        var preview = await _mediator.Send(
            new GetPayrollExportPreviewQuery(fromDate, untilDate, format, scope), cancellationToken);

        return Ok(preview);
    }

    [HttpPost]
    public async Task<IActionResult> Export([FromBody] PayrollExportFilter filter, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreatePayrollExportQuery(filter), cancellationToken);
        Response.Headers[ExportResponseHeaders.SkippedEntries] = result.SkippedEntryCount.ToString(CultureInfo.InvariantCulture);
        Response.Headers[ExportResponseHeaders.Persons] = result.PersonCount.ToString(CultureInfo.InvariantCulture);
        if (result.IsSupplementary)
        {
            Response.Headers[ExportResponseHeaders.Supplementary] = bool.TrueString;
        }

        if (result.AbsenceMappingInvalid)
        {
            Response.Headers[ExportResponseHeaders.AbsenceMappingInvalid] = bool.TrueString;
        }

        return File(result.FileContent, result.ContentType, result.FileName);
    }

    [HttpGet("{exportLogId:guid}/download")]
    public async Task<IActionResult> Download(Guid exportLogId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DownloadPayrollExportQuery(exportLogId), cancellationToken);
        return File(result.FileContent, result.ContentType, result.FileName);
    }
}
