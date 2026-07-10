// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handles date-range ZIP export queries: exports every sealed order with period-closed
/// work entries as one file per order plus a client period export entry, bundled into a
/// single ZIP archive.
/// @param request - Contains the date range, per-order format key and localization settings
/// </summary>
using System.IO.Compression;
using Klacks.Api.Application.Constants;
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

public class CreateOrderRangeExportQueryHandler : BaseTransactionHandler, IRequestHandler<CreateOrderRangeExportQuery, OrderExportResult>
{
    private const string ZipFileNamePattern = "erp-order-export_{0:yyyy-MM-dd}_{1:yyyy-MM-dd}.zip";
    private const string OrderEntryPattern = "order_{0}{1}";
    private const string ClientPeriodEntryPattern = "client-period-export_{0:yyyy-MM-dd}_{1:yyyy-MM-dd}{2}";
    private const string CollisionSuffixPattern = "{0}_{1}";
    private const char SanitizedReplacement = '-';

    private readonly ISealedOrderIdLoader _sealedOrderIdLoader;
    private readonly IOrderExportDataLoader _orderDataLoader;
    private readonly IClientPeriodExportDataLoader _clientPeriodDataLoader;
    private readonly IEnumerable<IExportFormatter> _formatters;
    private readonly IExportFormatPolicy _exportFormatPolicy;
    private readonly IClientPeriodExportFormatter _clientPeriodFormatter;
    private readonly IPeriodClosedEntryFilter _periodClosedEntryFilter;
    private readonly ICompanyInfoLoader _companyInfoLoader;
    private readonly IExportLogRepository _exportLogRepository;
    private readonly IExportFormatOverrideApplier _overrideApplier;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CreateOrderRangeExportQueryHandler(
        ISealedOrderIdLoader sealedOrderIdLoader,
        IOrderExportDataLoader orderDataLoader,
        IClientPeriodExportDataLoader clientPeriodDataLoader,
        IEnumerable<IExportFormatter> formatters,
        IExportFormatPolicy exportFormatPolicy,
        IClientPeriodExportFormatter clientPeriodFormatter,
        IPeriodClosedEntryFilter periodClosedEntryFilter,
        ICompanyInfoLoader companyInfoLoader,
        IExportLogRepository exportLogRepository,
        IExportFormatOverrideApplier overrideApplier,
        IHttpContextAccessor httpContextAccessor,
        IUnitOfWork unitOfWork,
        ILogger<CreateOrderRangeExportQueryHandler> logger) : base(unitOfWork, logger)
    {
        _sealedOrderIdLoader = sealedOrderIdLoader;
        _orderDataLoader = orderDataLoader;
        _clientPeriodDataLoader = clientPeriodDataLoader;
        _formatters = formatters;
        _exportFormatPolicy = exportFormatPolicy;
        _clientPeriodFormatter = clientPeriodFormatter;
        _periodClosedEntryFilter = periodClosedEntryFilter;
        _companyInfoLoader = companyInfoLoader;
        _exportLogRepository = exportLogRepository;
        _overrideApplier = overrideApplier;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<OrderExportResult> Handle(CreateOrderRangeExportQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithTransactionAsync(async () =>
        {
            var filter = request.Filter;

            if (string.IsNullOrWhiteSpace(filter.Format))
            {
                throw new InvalidRequestException("Export format must be specified.");
            }

            if (filter.FromDate > filter.UntilDate)
            {
                throw new InvalidRequestException("FromDate must not be after UntilDate.");
            }

            var formatter = _formatters.FirstOrDefault(f => f.FormatKey == filter.Format)
                ?? throw new InvalidRequestException($"Unknown export format: {filter.Format}");

            if (!await _exportFormatPolicy.IsEnabledAsync(filter.Format, cancellationToken))
            {
                throw new InvalidRequestException($"Export format is not enabled: {filter.Format}");
            }

            var orderIds = await _sealedOrderIdLoader.LoadIdsForRangeAsync(filter.FromDate, filter.UntilDate, cancellationToken);
            var orderData = await _orderDataLoader.LoadAsync(orderIds, filter.FromDate, filter.UntilDate, cancellationToken);
            var clientPeriodData = await _clientPeriodDataLoader.LoadAsync(filter.FromDate, filter.UntilDate, cancellationToken);

            var clientIds = CollectClientIds(orderData, clientPeriodData);
            var periodClosedLookup = await _periodClosedEntryFilter.BuildAsync(
                filter.FromDate, filter.UntilDate, clientIds, cancellationToken);

            var exportableOrders = FilterOrders(orderData.Orders, periodClosedLookup);
            FilterClientPeriodData(clientPeriodData, periodClosedLookup);

            var options = new ExportOptions
            {
                Language = filter.Language,
                CurrencyCode = filter.CurrencyCode,
                Company = await _companyInfoLoader.LoadAsync(cancellationToken)
            };

            var overrideApplied = await _overrideApplier.ApplyAsync(filter.Format, options, cancellationToken);

            var clientPeriodOptions = new ExportOptions
            {
                Language = filter.Language,
                CurrencyCode = filter.CurrencyCode,
                Company = options.Company
            };

            var clientPeriodOverrideApplied = await _overrideApplier.ApplyAsync(
                $"{ExportOverrideConstants.ClientPeriodFormatKeyPrefix}{_clientPeriodFormatter.FormatKey}", clientPeriodOptions, cancellationToken);

            var zipContent = BuildZipArchive(exportableOrders, clientPeriodData, formatter, options, clientPeriodOptions, filter);
            var fileName = string.Format(ZipFileNamePattern, filter.FromDate, filter.UntilDate);

            var userName = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";

            await _exportLogRepository.AddAsync(new ExportLog
            {
                Format = ExportConstants.RangeFormatPrefix + filter.Format,
                StartDate = filter.FromDate,
                EndDate = filter.UntilDate,
                GroupId = null,
                Language = filter.Language,
                CurrencyCode = filter.CurrencyCode,
                FileName = fileName,
                FileSize = zipContent.LongLength,
                RecordCount = exportableOrders.Count,
                ExportedAt = DateTime.UtcNow,
                ExportedBy = userName,
                OverrideApplied = overrideApplied || clientPeriodOverrideApplied
            }, cancellationToken);

            return new OrderExportResult
            {
                FileContent = zipContent,
                FileName = fileName,
                ContentType = ExportConstants.ContentTypeZip
            };
        }, "CreateOrderRangeExport", new { request.Filter.FromDate, request.Filter.UntilDate, request.Filter.Format });
    }

    private static IReadOnlyCollection<Guid> CollectClientIds(OrderExportData orderData, ClientPeriodExportData clientPeriodData)
    {
        var clientIds = new HashSet<Guid>();

        foreach (var order in orderData.Orders)
        {
            foreach (var work in order.WorkEntries)
            {
                clientIds.Add(work.EmployeeId);
            }
        }

        foreach (var client in clientPeriodData.Clients)
        {
            clientIds.Add(client.ClientId);
        }

        return clientIds;
    }

    private static List<OrderGroup> FilterOrders(List<OrderGroup> orders, IPeriodClosedLookup lookup)
    {
        var result = new List<OrderGroup>();

        foreach (var order in orders)
        {
            order.WorkEntries = order.WorkEntries
                .Where(w => lookup.IsClosed(w.EmployeeId, w.WorkDate))
                .ToList();

            foreach (var work in order.WorkEntries)
            {
                work.Breaks = work.Breaks
                    .Where(b => lookup.IsClosed(work.EmployeeId, b.BreakDate))
                    .ToList();
            }

            if (order.WorkEntries.Count > 0)
            {
                result.Add(order);
            }
        }

        return result;
    }

    private static void FilterClientPeriodData(ClientPeriodExportData data, IPeriodClosedLookup lookup)
    {
        foreach (var client in data.Clients)
        {
            client.WorkEntries = client.WorkEntries
                .Where(w => lookup.IsClosed(client.ClientId, w.WorkDate))
                .ToList();

            foreach (var work in client.WorkEntries)
            {
                work.Breaks = work.Breaks
                    .Where(b => lookup.IsClosed(client.ClientId, b.BreakDate))
                    .ToList();
            }
        }

        data.Clients = data.Clients.Where(c => c.WorkEntries.Count > 0).ToList();
    }

    private byte[] BuildZipArchive(
        List<OrderGroup> orders,
        ClientPeriodExportData clientPeriodData,
        IExportFormatter formatter,
        ExportOptions options,
        ExportOptions clientPeriodOptions,
        OrderRangeExportFilter filter)
    {
        using var memoryStream = new MemoryStream();

        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var usedEntryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var order in orders)
            {
                var singleOrderData = new OrderExportData
                {
                    Orders = [order],
                    StartDate = filter.FromDate,
                    EndDate = filter.UntilDate,
                };

                var entryName = BuildOrderEntryName(order, formatter.FileExtension, usedEntryNames);
                WriteZipEntry(archive, entryName, formatter.Format(singleOrderData, options));
            }

            var clientPeriodEntryName = string.Format(
                ClientPeriodEntryPattern, filter.FromDate, filter.UntilDate, _clientPeriodFormatter.FileExtension);
            WriteZipEntry(archive, clientPeriodEntryName, _clientPeriodFormatter.Format(clientPeriodData, clientPeriodOptions));
        }

        return memoryStream.ToArray();
    }

    private static string BuildOrderEntryName(OrderGroup order, string extension, HashSet<string> usedEntryNames)
    {
        var label = !string.IsNullOrWhiteSpace(order.ExternalOrderReference)
            ? order.ExternalOrderReference
            : !string.IsNullOrWhiteSpace(order.OrderAbbreviation)
                ? order.OrderAbbreviation
                : order.OrderShiftId.ToString();

        var safe = Sanitize(label);
        var entryName = string.Format(OrderEntryPattern, safe, extension);

        var suffix = 2;
        while (!usedEntryNames.Add(entryName))
        {
            entryName = string.Format(OrderEntryPattern, string.Format(CollisionSuffixPattern, safe, suffix), extension);
            suffix++;
        }

        return entryName;
    }

    private static string Sanitize(string value)
    {
        var chars = value.Select(c => char.IsAsciiLetterOrDigit(c) || c == '.' || c == '_' || c == '-'
            ? c
            : SanitizedReplacement).ToArray();
        return new string(chars);
    }

    private static void WriteZipEntry(ZipArchive archive, string entryName, byte[] content)
    {
        var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        using var entryStream = entry.Open();
        entryStream.Write(content, 0, content.Length);
    }
}
