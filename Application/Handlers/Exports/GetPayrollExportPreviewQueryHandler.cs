// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Answers what a payroll export of a period would do right now (blockers, new or changed persons) without writing.
/// </summary>
/// <param name="exportService">The single payroll export path</param>
/// <param name="logger">Logger of the handler base</param>
using Klacks.Api.Application.DTOs.Exports;
using Klacks.Api.Application.Interfaces.Exports;
using Klacks.Api.Application.Queries.Exports;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Exports;

public class GetPayrollExportPreviewQueryHandler : BaseHandler, IRequestHandler<GetPayrollExportPreviewQuery, PayrollExportPreviewDto>
{
    private readonly IPayrollPeriodExportService _exportService;

    public GetPayrollExportPreviewQueryHandler(
        IPayrollPeriodExportService exportService,
        ILogger<GetPayrollExportPreviewQueryHandler> logger) : base(logger)
    {
        _exportService = exportService;
    }

    public async Task<PayrollExportPreviewDto> Handle(GetPayrollExportPreviewQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(
            () => _exportService.PreviewAsync(
                request.FromDate, request.UntilDate, request.Format, request.ClientIds, cancellationToken),
            "GetPayrollExportPreview",
            new { request.FromDate, request.UntilDate, request.Format });
    }
}
