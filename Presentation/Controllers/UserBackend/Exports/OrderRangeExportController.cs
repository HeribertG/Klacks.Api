// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Controller for exporting all sealed orders of a date range as a ZIP archive with one
/// file per order plus a client period export, restricted to period-closed entries.
/// </summary>
using Klacks.Api.Application.DTOs.Exports;
using Klacks.Api.Application.Queries.Exports;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Exports;

public class OrderRangeExportController : BaseController
{
    private readonly IMediator _mediator;

    public OrderRangeExportController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Export([FromBody] OrderRangeExportFilter filter, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateOrderRangeExportQuery(filter), cancellationToken);
        return File(result.FileContent, result.ContentType, result.FileName);
    }
}
