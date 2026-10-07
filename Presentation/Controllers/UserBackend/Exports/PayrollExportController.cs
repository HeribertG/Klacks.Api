// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Controller for a manual, on-demand payroll (country-pack) export of a single group's closed
/// period. Complements the automatic period-closed payroll export by letting an admin re-run the
/// export for a chosen group, date range and format. Restricted to admins: the file holds the wages-relevant
/// time values of a whole group. Entries the formatter could not write are reported in the
/// ExportResponseHeaders.SkippedEntries header (and an unparsable absence mapping in AbsenceMappingInvalid).
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

public class PayrollExportController : BaseController
{
    private readonly IMediator _mediator;

    public PayrollExportController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
    public async Task<IActionResult> Export([FromBody] PayrollExportFilter filter, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreatePayrollExportQuery(filter), cancellationToken);
        Response.Headers[ExportResponseHeaders.SkippedEntries] = result.SkippedEntryCount.ToString(CultureInfo.InvariantCulture);
        if (result.AbsenceMappingInvalid)
        {
            Response.Headers[ExportResponseHeaders.AbsenceMappingInvalid] = bool.TrueString;
        }

        return File(result.FileContent, result.ContentType, result.FileName);
    }
}
