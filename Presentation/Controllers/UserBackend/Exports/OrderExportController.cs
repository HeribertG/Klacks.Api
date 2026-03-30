// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Controller for exporting closed work entries grouped by order (shift).
/// Supports multiple export formats (CSV, JSON, XML) for ERP and accounting integration.
/// </summary>
using Klacks.Api.Application.DTOs.Exports;
using Klacks.Api.Application.Queries.Exports;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Exports;

public class OrderExportController : BaseController
{
    private readonly IMediator _mediator;

    public OrderExportController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Export([FromBody] OrderExportFilter filter, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateOrderExportQuery(filter), cancellationToken);
        return File(result.FileContent, result.ContentType, result.FileName);
    }
}
