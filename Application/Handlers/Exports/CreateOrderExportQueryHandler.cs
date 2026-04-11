// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handles order export queries by loading data and delegating to the appropriate formatter.
/// @param request - Contains filter with date range, format key and localization
/// </summary>
using Klacks.Api.Application.DTOs.Exports;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Exports;
using Klacks.Api.Application.Queries.Exports;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Exports;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Klacks.Api.Application.Handlers.Exports;

public class CreateOrderExportQueryHandler : BaseTransactionHandler, IRequestHandler<CreateOrderExportQuery, OrderExportResult>
{
    private readonly IOrderExportDataLoader _dataLoader;
    private readonly IEnumerable<IExportFormatter> _formatters;
    private readonly ISettingsReader _settingsReader;
    private readonly IExportLogRepository _exportLogRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CreateOrderExportQueryHandler(
        IOrderExportDataLoader dataLoader,
        IEnumerable<IExportFormatter> formatters,
        ISettingsReader settingsReader,
        IExportLogRepository exportLogRepository,
        IHttpContextAccessor httpContextAccessor,
        IUnitOfWork unitOfWork,
        ILogger<CreateOrderExportQueryHandler> logger) : base(unitOfWork, logger)
    {
        _dataLoader = dataLoader;
        _formatters = formatters;
        _settingsReader = settingsReader;
        _exportLogRepository = exportLogRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<OrderExportResult> Handle(CreateOrderExportQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithTransactionAsync(async () =>
        {
            var filter = request.Filter;

            if (string.IsNullOrWhiteSpace(filter.Format))
            {
                throw new InvalidRequestException("Export format must be specified.");
            }

            if (filter.StartDate > filter.EndDate)
            {
                throw new InvalidRequestException("Start date must be before or equal to end date.");
            }

            var formatter = _formatters.FirstOrDefault(f => f.FormatKey == filter.Format)
                ?? throw new InvalidRequestException($"Unknown export format: {filter.Format}");

            var exportData = await _dataLoader.LoadAsync(filter.StartDate, filter.EndDate, cancellationToken);
            var companyInfo = await LoadCompanyInfoAsync();

            var options = new ExportOptions
            {
                Language = filter.Language,
                CurrencyCode = filter.CurrencyCode,
                Company = companyInfo
            };

            var fileContent = formatter.Format(exportData, options);
            var fileName = $"order-export_{filter.StartDate:yyyy-MM-dd}_{filter.EndDate:yyyy-MM-dd}{formatter.FileExtension}";

            var userName = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";

            await _exportLogRepository.AddAsync(new ExportLog
            {
                Format = filter.Format,
                StartDate = filter.StartDate,
                EndDate = filter.EndDate,
                GroupId = filter.GroupId,
                Language = filter.Language,
                CurrencyCode = filter.CurrencyCode,
                FileName = fileName,
                FileSize = fileContent.LongLength,
                RecordCount = exportData.Orders?.Count ?? 0,
                ExportedAt = DateTime.UtcNow,
                ExportedBy = userName
            }, cancellationToken);

            return new OrderExportResult
            {
                FileContent = fileContent,
                FileName = fileName,
                ContentType = formatter.ContentType
            };
        }, "CreateOrderExport", new { request.Filter.StartDate, request.Filter.EndDate, request.Filter.Format });
    }

    private async Task<CompanyInfo> LoadCompanyInfoAsync()
    {
        var nameSetting = await _settingsReader.GetSetting(Constants.Settings.APP_ADDRESS_NAME);
        var taxIdSetting = await _settingsReader.GetSetting(Constants.Settings.COMPANY_TAX_ID);
        var vatIdSetting = await _settingsReader.GetSetting(Constants.Settings.COMPANY_VAT_ID);
        var commercialRegisterSetting = await _settingsReader.GetSetting(Constants.Settings.COMPANY_COMMERCIAL_REGISTER);

        return new CompanyInfo
        {
            Name = nameSetting?.Value ?? string.Empty,
            TaxId = taxIdSetting?.Value ?? string.Empty,
            VatId = vatIdSetting?.Value ?? string.Empty,
            CommercialRegister = commercialRegisterSetting?.Value ?? string.Empty
        };
    }
}
