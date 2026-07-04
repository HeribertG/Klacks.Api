// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Controller for exporting closed work entries grouped by client (employee/external employee)
/// for a given date range. Used for period-closing exports.
/// </summary>
using Klacks.Api.Application.DTOs.Exports;
using Klacks.Api.Application.Queries.Exports;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Exports;

public class ClientPeriodExportController : BaseController
{
    private readonly IMediator _mediator;

    public ClientPeriodExportController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Export([FromBody] ClientPeriodExportFilter filter, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateClientPeriodExportQuery(filter), cancellationToken);
        return File(result.FileContent, result.ContentType, result.FileName);
    }
}
