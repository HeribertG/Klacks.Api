// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Returns the stored artifact of an earlier payroll export run. Only payroll export logs that carry a storage key
/// under the payroll prefix qualify; an unknown id, an order export log, a log without artifact and an artifact that
/// has vanished from the storage are all answered with the same not-found result (404), so nothing about other
/// exports leaks. The content type comes from the registered formatter of the logged format.
/// </summary>
/// <param name="exportLogRepository">Loads the export log row</param>
/// <param name="formatters">Registered payroll formatters, used for the content type of the logged format</param>
/// <param name="artifactStorage">Holds the stored artifact; reports a missing file as null</param>
/// <param name="httpContextAccessor">Supplies the acting user for the re-download log line</param>
/// <param name="logger">Logger of the handler base</param>
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Exports;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Exports;
using Klacks.Api.Application.Queries.Exports;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Klacks.Api.Application.Handlers.Exports;

public class DownloadPayrollExportQueryHandler : BaseHandler, IRequestHandler<DownloadPayrollExportQuery, PayrollExportOutcome>
{
    private const string NotFoundMessage = "Payroll export not found.";

    private readonly IExportLogRepository _exportLogRepository;
    private readonly IEnumerable<IPayrollExportFormatter> _formatters;
    private readonly IPayrollArtifactStorage _artifactStorage;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DownloadPayrollExportQueryHandler(
        IExportLogRepository exportLogRepository,
        IEnumerable<IPayrollExportFormatter> formatters,
        IPayrollArtifactStorage artifactStorage,
        IHttpContextAccessor httpContextAccessor,
        ILogger<DownloadPayrollExportQueryHandler> logger) : base(logger)
    {
        _exportLogRepository = exportLogRepository;
        _formatters = formatters;
        _artifactStorage = artifactStorage;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<PayrollExportOutcome> Handle(DownloadPayrollExportQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var log = await _exportLogRepository.GetByIdAsync(request.ExportLogId, cancellationToken);

            if (log is null || !IsPayrollArtifact(log.StorageKey))
            {
                throw new KeyNotFoundException(NotFoundMessage);
            }

            var content = await _artifactStorage.ReadAsync(log.StorageKey!, cancellationToken);
            if (content is null)
            {
                _logger.LogWarning(
                    "Payroll export artifact of export log {ExportLogId} is missing from the artifact storage.",
                    log.Id);
                throw new KeyNotFoundException(NotFoundMessage);
            }

            var actor = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? PayrollExportConstants.UnknownActor;
            _logger.LogInformation(
                "Payroll export {ExportLogId} downloaded again by user {UserId}.", log.Id, actor);

            var formatter = _formatters.FirstOrDefault(f => f.FormatKey == log.Format);

            return new PayrollExportOutcome
            {
                FileContent = content,
                FileName = log.FileName,
                ContentType = formatter?.ContentType ?? PayrollExportConstants.FallbackContentType,
                SkippedEntryCount = log.SkippedAbsenceCount
                    + log.SkippedUnsupportedUnitCount
                    + log.SkippedUnsupportedKindCount
                    + log.SkippedUnmappedSurchargeCount
                    + log.SkippedUnmappedBaseWageCount
                    + log.SkippedSupersededCount,
                AbsenceMappingInvalid = log.AbsenceMappingInvalid,
                PersonCount = log.PersonCount,
                IsSupplementary = log.IsSupplementary,
                ExportLogId = log.Id
            };
        }, "DownloadPayrollExport", new { request.ExportLogId });
    }

    private static bool IsPayrollArtifact(string? storageKey)
    {
        return !string.IsNullOrWhiteSpace(storageKey)
            && storageKey.StartsWith(PayrollExportConstants.StorageKeyPrefix + "/", StringComparison.Ordinal);
    }
}
