// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Controller for spreadsheet exports of a report. The rows arrive already resolved, because the
/// data providers that read them live in the frontend.
/// </summary>
using Klacks.Api.Application.Commands.Reports;
using Klacks.Api.Application.DTOs.Reports;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Reports;

[ApiController]
public class ReportExportController : BaseController
{
    private readonly IMediator _mediator;

    public ReportExportController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("xlsx")]
    public async Task<IActionResult> Xlsx([FromBody] ReportXlsxRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateReportXlsxCommand(request), cancellationToken);
        return File(result.FileContent, result.ContentType, result.FileName);
    }
}
