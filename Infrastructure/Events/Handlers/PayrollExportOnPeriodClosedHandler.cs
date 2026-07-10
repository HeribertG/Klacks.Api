// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Country-pack payroll-export hook. Reacts to a committed period seal: when the pack is enabled and
/// the seal is group-scoped, it resolves the group's configured target system, loads the closed period
/// data, formats it with the matching registered formatter and stores the artifact for later UI download,
/// writing an ExportLog entry. Runs post-commit and non-blocking; a failure is logged and never affects
/// the already-committed seal. Adding a country pack is purely additive: register a new
/// IPayrollExportFormatter and point a group's PayrollExportGroupConfig.TargetSystem at its FormatKey.
/// </summary>
/// <param name="featurePluginService">Reads whether the country-pack subsystem is enabled (feature-plugin gate)</param>
/// <param name="mediator">Loads the employee-centric, day-granular payroll data of the closed period</param>
/// <param name="configRepository">Resolves the per-group export configuration (target system, mapping, delimiter, encoding)</param>
/// <param name="formatters">Registered payroll formatters; the one whose FormatKey matches the group's TargetSystem is selected</param>
/// <param name="exportFormatPolicy">Admin-configurable enable/disable gate shared with the order export catalog</param>
/// <param name="objectStorage">Stores the generated artifact for later retrieval/download</param>
/// <param name="exportLogRepository">Idempotency guard and audit trail for payroll exports</param>
/// <param name="unitOfWork">Persists the ExportLog entry (the hook runs outside the seal transaction)</param>
/// <param name="logger">Logs hook activity, skipped absences and failures</param>

using System.Globalization;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Exports;
using Klacks.Api.Application.Interfaces.Plugins;
using Klacks.Api.Application.Queries.Exports;
using Klacks.Api.Domain.Events;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Domain.Interfaces.Imports;
using Klacks.Api.Domain.Models.Exports;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Events.Handlers;

public sealed class PayrollExportOnPeriodClosedHandler : IDomainEventHandler<PeriodClosedEvent>
{
    public const string FeaturePluginName = PayrollExportConstants.FeaturePluginName;

    private const string ExportLanguage = "de";
    private const string StorageKeyPrefix = "payroll-export";
    private const string PeriodDateFormat = "yyyyMMdd";

    private readonly IFeaturePluginService _featurePluginService;
    private readonly IMediator _mediator;
    private readonly IPayrollExportConfigRepository _configRepository;
    private readonly IEnumerable<IPayrollExportFormatter> _formatters;
    private readonly IExportFormatPolicy _exportFormatPolicy;
    private readonly IObjectStorageService _objectStorage;
    private readonly IExportLogRepository _exportLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PayrollExportOnPeriodClosedHandler> _logger;

    public PayrollExportOnPeriodClosedHandler(
        IFeaturePluginService featurePluginService,
        IMediator mediator,
        IPayrollExportConfigRepository configRepository,
        IEnumerable<IPayrollExportFormatter> formatters,
        IExportFormatPolicy exportFormatPolicy,
        IObjectStorageService objectStorage,
        IExportLogRepository exportLogRepository,
        IUnitOfWork unitOfWork,
        ILogger<PayrollExportOnPeriodClosedHandler> logger)
    {
        _featurePluginService = featurePluginService;
        _mediator = mediator;
        _configRepository = configRepository;
        _formatters = formatters;
        _exportFormatPolicy = exportFormatPolicy;
        _objectStorage = objectStorage;
        _exportLogRepository = exportLogRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task HandleAsync(PeriodClosedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (!_featurePluginService.IsEnabled(FeaturePluginName))
        {
            return;
        }

        if (domainEvent.GroupId is null)
        {
            _logger.LogInformation(
                "Payroll-export pack '{Plugin}' skipped period {Start}..{End}: no group scope (a full-period close cannot resolve a group).",
                FeaturePluginName,
                domainEvent.StartDate,
                domainEvent.EndDate);
            return;
        }

        var groupId = domainEvent.GroupId.Value;
        var config = await _configRepository.GetByGroupAsync(groupId, cancellationToken);

        if (await AlreadyExportedAsync(groupId, config.TargetSystem, domainEvent.StartDate, domainEvent.EndDate, cancellationToken))
        {
            _logger.LogInformation(
                "Payroll-export pack '{Plugin}' skipped period {Start}..{End} (group {GroupId}): already exported.",
                FeaturePluginName,
                domainEvent.StartDate,
                domainEvent.EndDate,
                groupId);
            return;
        }

        var formatter = _formatters.FirstOrDefault(f => f.FormatKey == config.TargetSystem);
        if (formatter is null)
        {
            _logger.LogWarning(
                "Payroll-export pack '{Plugin}': no formatter registered for target system '{TargetSystem}' (group {GroupId}); nothing exported.",
                FeaturePluginName,
                config.TargetSystem,
                groupId);
            return;
        }

        if (!await _exportFormatPolicy.IsEnabledAsync(config.TargetSystem, cancellationToken))
        {
            _logger.LogInformation(
                "Payroll-export pack '{Plugin}': target system '{TargetSystem}' is disabled in export format settings (group {GroupId}); nothing exported.",
                FeaturePluginName,
                config.TargetSystem,
                groupId);
            return;
        }

        var data = await _mediator.Send(
            new GetPayrollPeriodDataQuery(groupId, domainEvent.StartDate, domainEvent.EndDate),
            cancellationToken);

        if (data.Employees.Count == 0)
        {
            _logger.LogInformation(
                "Payroll-export pack '{Plugin}': period {Start}..{End} (group {GroupId}) has no closed employee time values; nothing exported.",
                FeaturePluginName,
                domainEvent.StartDate,
                domainEvent.EndDate,
                groupId);
            return;
        }

        Domain.Models.Exports.Payroll.PayrollExportResult result;
        try
        {
            result = formatter.Format(data, config);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Payroll-export pack '{Plugin}': formatter '{TargetSystem}' failed for period {Start}..{End} (group {GroupId}); nothing exported. The seal itself is unaffected.",
                FeaturePluginName,
                config.TargetSystem,
                domainEvent.StartDate,
                domainEvent.EndDate,
                groupId);
            return;
        }

        var fileName = BuildFileName(groupId, domainEvent, formatter.FileExtension);
        var storageKey = BuildStorageKey(groupId, domainEvent, formatter.FileExtension);

        using (var stream = new MemoryStream(result.Content))
        {
            await _objectStorage.UploadAsync(storageKey, stream, cancellationToken);
        }

        await _exportLogRepository.AddAsync(
            new ExportLog
            {
                Format = config.TargetSystem,
                StartDate = domainEvent.StartDate,
                EndDate = domainEvent.EndDate,
                GroupId = groupId,
                Language = ExportLanguage,
                FileName = fileName,
                FileSize = result.Content.LongLength,
                RecordCount = result.RecordCount,
                ExportedAt = DateTime.UtcNow,
                ExportedBy = domainEvent.SealedBy,
            },
            cancellationToken);

        await _unitOfWork.CompleteAsync();

        if (result.SkippedAbsenceCount > 0)
        {
            _logger.LogWarning(
                "Payroll-export pack '{Plugin}': {SkippedAbsenceCount} absence entries were skipped for period {Start}..{End} (group {GroupId}) because no mapping exists in the group config.",
                FeaturePluginName,
                result.SkippedAbsenceCount,
                domainEvent.StartDate,
                domainEvent.EndDate,
                groupId);
        }

        _logger.LogInformation(
            "Payroll-export pack '{Plugin}': exported period {Start}..{End} (group {GroupId}) as '{FileName}' with {RecordCount} lines.",
            FeaturePluginName,
            domainEvent.StartDate,
            domainEvent.EndDate,
            groupId,
            fileName,
            result.RecordCount);
    }

    private async Task<bool> AlreadyExportedAsync(
        Guid groupId,
        string targetSystem,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken)
    {
        var existing = await _exportLogRepository.GetRangeAsync(startDate, endDate, cancellationToken);
        return existing.Any(l =>
            l.GroupId == groupId
            && l.Format == targetSystem
            && l.StartDate == startDate
            && l.EndDate == endDate);
    }

    private static string BuildFileName(Guid groupId, PeriodClosedEvent domainEvent, string extension)
    {
        var start = domainEvent.StartDate.ToString(PeriodDateFormat, CultureInfo.InvariantCulture);
        var end = domainEvent.EndDate.ToString(PeriodDateFormat, CultureInfo.InvariantCulture);
        return $"{FeaturePluginName}_{groupId}_{start}-{end}{extension}";
    }

    private static string BuildStorageKey(Guid groupId, PeriodClosedEvent domainEvent, string extension)
    {
        var start = domainEvent.StartDate.ToString(PeriodDateFormat, CultureInfo.InvariantCulture);
        var end = domainEvent.EndDate.ToString(PeriodDateFormat, CultureInfo.InvariantCulture);
        return $"{StorageKeyPrefix}/{groupId}/{start}-{end}{extension}";
    }
}
