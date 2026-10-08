// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handles manual, on-demand payroll exports of a period. Thin adapter over IPayrollPeriodExportService, which
/// owns validation, the completeness gate, the new-or-changed selection, the stored artifact and the export log;
/// the handler only supplies the acting user from the HTTP context. The export is person-based and not group-scoped.
/// </summary>
/// <param name="exportService">The single payroll export path</param>
/// <param name="httpContextAccessor">Supplies the acting user stored as ExportedBy</param>
/// <param name="logger">Logger of the handler base</param>
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Exports;
using Klacks.Api.Application.Interfaces.Exports;
using Klacks.Api.Application.Queries.Exports;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Klacks.Api.Application.Handlers.Exports;

public class CreatePayrollExportQueryHandler : BaseHandler, IRequestHandler<CreatePayrollExportQuery, PayrollExportOutcome>
{
    private readonly IPayrollPeriodExportService _exportService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CreatePayrollExportQueryHandler(
        IPayrollPeriodExportService exportService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<CreatePayrollExportQueryHandler> logger) : base(logger)
    {
        _exportService = exportService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<PayrollExportOutcome> Handle(CreatePayrollExportQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var filter = request.Filter;
            var actor = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? PayrollExportConstants.UnknownActor;

            return await _exportService.ExportAsync(
                filter.FromDate,
                filter.UntilDate,
                filter.Format,
                filter.Language,
                filter.ClientIds,
                actor,
                cancellationToken);
        }, "CreatePayrollExport", new { request.Filter.FromDate, request.Filter.UntilDate, request.Filter.Format });
    }
}
