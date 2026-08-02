// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Builds the Excel workbook of a report from the values the client already resolved.
/// </summary>
/// <param name="request">Sheets, columns and rows of the report to export</param>
using Klacks.Api.Application.Commands.Reports;
using Klacks.Api.Application.DTOs.Reports;
using Klacks.Api.Application.Interfaces.Exports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Reports;

public class CreateReportXlsxCommandHandler : BaseHandler,
    IRequestHandler<CreateReportXlsxCommand, ReportExportResult>
{
    private readonly IReportXlsxBuilder _builder;

    public CreateReportXlsxCommandHandler(
        IReportXlsxBuilder builder,
        ILogger<CreateReportXlsxCommandHandler> logger)
        : base(logger)
    {
        _builder = builder;
    }

    public Task<ReportExportResult> Handle(CreateReportXlsxCommand request, CancellationToken cancellationToken)
    {
        return ExecuteAsync(
            () => Task.FromResult(_builder.Build(request.Request)),
            nameof(CreateReportXlsxCommand),
            new { Sheets = request.Request.Sheets.Count });
    }
}
