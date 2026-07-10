// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handles manual, on-demand payroll exports for a single group's closed period. Resolves the
/// country-pack formatter by the requested format key, loads the group's per-group configuration
/// (wage-type/absence mapping) and the closed period data, then produces the file and writes an
/// ExportLog audit row. Unlike the automatic period-closed path this returns the bytes directly
/// for download instead of uploading them to object storage.
/// @param request - Contains the filter with group, date range, localization and format key
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

public class CreatePayrollExportQueryHandler : BaseTransactionHandler, IRequestHandler<CreatePayrollExportQuery, OrderExportResult>
{
    private readonly IMediator _mediator;
    private readonly IEnumerable<IPayrollExportFormatter> _formatters;
    private readonly IPayrollExportConfigRepository _configRepository;
    private readonly IExportFormatPolicy _exportFormatPolicy;
    private readonly IExportLogRepository _exportLogRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CreatePayrollExportQueryHandler(
        IMediator mediator,
        IEnumerable<IPayrollExportFormatter> formatters,
        IPayrollExportConfigRepository configRepository,
        IExportFormatPolicy exportFormatPolicy,
        IExportLogRepository exportLogRepository,
        IHttpContextAccessor httpContextAccessor,
        IUnitOfWork unitOfWork,
        ILogger<CreatePayrollExportQueryHandler> logger) : base(unitOfWork, logger)
    {
        _mediator = mediator;
        _formatters = formatters;
        _configRepository = configRepository;
        _exportFormatPolicy = exportFormatPolicy;
        _exportLogRepository = exportLogRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<OrderExportResult> Handle(CreatePayrollExportQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithTransactionAsync(async () =>
        {
            var filter = request.Filter;

            if (filter.GroupId == Guid.Empty)
            {
                throw new InvalidRequestException("Group must be specified for a payroll export.");
            }

            if (filter.FromDate > filter.UntilDate)
            {
                throw new InvalidRequestException("FromDate must not be after UntilDate.");
            }

            if (string.IsNullOrWhiteSpace(filter.Format))
            {
                throw new InvalidRequestException("Export format must be specified.");
            }

            var formatter = _formatters.FirstOrDefault(f => f.FormatKey == filter.Format)
                ?? throw new InvalidRequestException($"Unknown payroll export format: {filter.Format}");

            if (!await _exportFormatPolicy.IsEnabledAsync(filter.Format, cancellationToken))
            {
                throw new InvalidRequestException($"Payroll export format is not enabled: {filter.Format}");
            }

            var config = await _configRepository.GetByGroupAsync(filter.GroupId, cancellationToken);

            var data = await _mediator.Send(
                new GetPayrollPeriodDataQuery(filter.GroupId, filter.FromDate, filter.UntilDate),
                cancellationToken);

            if (data.Employees.Count == 0)
            {
                throw new InvalidRequestException(
                    "No closed payroll data for the selected group and period. Seal the period first.");
            }

            var result = formatter.Format(data, config);
            var fileName = $"payroll-export_{filter.FromDate:yyyy-MM-dd}_{filter.UntilDate:yyyy-MM-dd}{formatter.FileExtension}";

            var userName = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";

            await _exportLogRepository.AddAsync(new ExportLog
            {
                Format = filter.Format,
                StartDate = filter.FromDate,
                EndDate = filter.UntilDate,
                GroupId = filter.GroupId,
                Language = filter.Language,
                FileName = fileName,
                FileSize = result.Content.LongLength,
                RecordCount = result.RecordCount,
                ExportedAt = DateTime.UtcNow,
                ExportedBy = userName
            }, cancellationToken);

            return new OrderExportResult
            {
                FileContent = result.Content,
                FileName = fileName,
                ContentType = formatter.ContentType
            };
        }, "CreatePayrollExport", new { request.Filter.GroupId, request.Filter.FromDate, request.Filter.UntilDate });
    }
}
