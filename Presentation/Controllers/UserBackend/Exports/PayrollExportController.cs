// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Controller for a manual, on-demand payroll (country-pack) export of a single group's closed
/// period. Complements the automatic period-closed payroll export by letting an admin re-run the
/// export for a chosen group, date range and format.
/// </summary>
using Klacks.Api.Application.DTOs.Exports;
using Klacks.Api.Application.Queries.Exports;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Exports;

public class PayrollExportController : BaseController
{
    private readonly IMediator _mediator;

    public PayrollExportController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Export([FromBody] PayrollExportFilter filter, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreatePayrollExportQuery(filter), cancellationToken);
        return File(result.FileContent, result.ContentType, result.FileName);
    }
}
