// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Controller for spreadsheet exports of a report. The rows arrive already resolved, because the
/// data providers that read them live in the frontend.
/// </summary>
/// <remarks>
/// Deliberately available to every authenticated user rather than admins only: printing and
/// exporting a report one is allowed to see is not an administrative action, and the caller
/// supplies the data itself. The size limit bounds what a single request may allocate.
/// </remarks>
using Klacks.Api.Application.Commands.Reports;
using Klacks.Api.Application.DTOs.Reports;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Reports;

[ApiController]
public class ReportExportController : BaseController
{
    private const int MaxRequestSizeBytes = 25 * 1024 * 1024;

    private readonly IMediator _mediator;

    public ReportExportController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("xlsx")]
    [RequestSizeLimit(MaxRequestSizeBytes)]
    public async Task<IActionResult> Xlsx([FromBody] ReportXlsxRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateReportXlsxCommand(request), cancellationToken);
        return File(result.FileContent, result.ContentType, result.FileName);
    }
}
