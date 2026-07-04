// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handles client period export queries by loading data grouped by client and delegating to the formatter.
/// @param request - Contains filter with date range and localization
/// </summary>
using Klacks.Api.Application.DTOs.Exports;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Exports;
using Klacks.Api.Application.Queries.Exports;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Domain.Models.Exports;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Klacks.Api.Application.Handlers.Exports;

public class CreateClientPeriodExportQueryHandler : BaseTransactionHandler, IRequestHandler<CreateClientPeriodExportQuery, OrderExportResult>
{
    private readonly IClientPeriodExportDataLoader _dataLoader;
    private readonly IClientPeriodExportFormatter _formatter;
    private readonly IExportLogRepository _exportLogRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CreateClientPeriodExportQueryHandler(
        IClientPeriodExportDataLoader dataLoader,
        IClientPeriodExportFormatter formatter,
        IExportLogRepository exportLogRepository,
        IHttpContextAccessor httpContextAccessor,
        IUnitOfWork unitOfWork,
        ILogger<CreateClientPeriodExportQueryHandler> logger) : base(unitOfWork, logger)
    {
        _dataLoader = dataLoader;
        _formatter = formatter;
        _exportLogRepository = exportLogRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<OrderExportResult> Handle(CreateClientPeriodExportQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithTransactionAsync(async () =>
        {
            var filter = request.Filter;

            if (filter.FromDate > filter.UntilDate)
            {
                throw new InvalidRequestException("FromDate must not be after UntilDate.");
            }

            var exportData = await _dataLoader.LoadAsync(filter.FromDate, filter.UntilDate, cancellationToken);

            var options = new ExportOptions
            {
                Language = filter.Language,
                CurrencyCode = filter.CurrencyCode
            };

            var fileContent = _formatter.Format(exportData, options);
            var fileName = $"client-period-export_{filter.FromDate:yyyy-MM-dd}_{filter.UntilDate:yyyy-MM-dd}{_formatter.FileExtension}";

            var userName = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";

            await _exportLogRepository.AddAsync(new ExportLog
            {
                Format = Klacks.Api.Application.Constants.ExportConstants.FormatClientPeriodXml,
                StartDate = exportData.StartDate,
                EndDate = exportData.EndDate,
                GroupId = null,
                Language = filter.Language,
                CurrencyCode = filter.CurrencyCode,
                FileName = fileName,
                FileSize = fileContent.LongLength,
                RecordCount = exportData.Clients.Count,
                ExportedAt = DateTime.UtcNow,
                ExportedBy = userName
            }, cancellationToken);

            return new OrderExportResult
            {
                FileContent = fileContent,
                FileName = fileName,
                ContentType = _formatter.ContentType
            };
        }, "CreateClientPeriodExport", new { request.Filter.FromDate, request.Filter.UntilDate });
    }
}
