// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The single path for payroll exports of a period. Resolves and checks the format, lets the completeness gate
/// decide whether the period may be exported, loads the person-based closed data, compares each person's content
/// hash with the latest export of that person for the same period and format and exports only persons that are new
/// or changed. A run that contains at least one person with an earlier export is a supplementary export (file name
/// suffix, ExportLog.IsSupplementary).
/// Order and failure handling: the artifact is uploaded first, under a key that contains the pre-assigned
/// ExportLog id, and then the ExportLog row and one ExportLogItem per person are committed with a single
/// SaveChanges (one database transaction). When the commit fails the uploaded artifact is deleted again (best
/// effort). A log row therefore never points at a missing file; the worst case (process death between upload and
/// commit, or a failed delete) is an unreferenced file under a unique key, which is harmless. A concurrent export
/// of the same persons computes the same next revision and violates the unique revision index; this surfaces as
/// PayrollExportConcurrentException. The service is called by the request handlers; a future automatic trigger calls
/// ExportAsync directly with its own actor.
/// </summary>
/// <param name="formatters">Registered payroll formatters; the one whose FormatKey matches the request is used</param>
/// <param name="exportFormatPolicy">Admin-configurable enable/disable gate of the export formats</param>
/// <param name="completenessGate">Decides whether the period is complete enough to be exported</param>
/// <param name="dataLoader">Loads the person-based closed payroll data of the period</param>
/// <param name="exportLogItemRepository">Per-person export history (latest revision and hash, new items)</param>
/// <param name="exportLogRepository">Export history rows</param>
/// <param name="configRepository">Installation-wide payroll export configuration</param>
/// <param name="overrideApplier">Applies the admin format override to the configuration</param>
/// <param name="artifactStorage">Stores the generated artifact for re-download, apart from the ERP drop zone</param>
/// <param name="unitOfWork">Commits the ExportLog row and its items in one transaction</param>
/// <param name="logger">Logs the run and artifact cleanup problems</param>
using System.Globalization;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Exports;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Exports;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Domain.Models.Exports;
using Klacks.Api.Domain.Models.Exports.Payroll;
using Klacks.Api.Domain.Services.Exports;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services.Exports;

public class PayrollPeriodExportService : IPayrollPeriodExportService
{
    private const string NoClosedDataMessage =
        "No closed payroll data for the selected period. Seal the period first.";

    private const string FromAfterUntilMessage = "FromDate must not be after UntilDate.";

    private static readonly string PeriodTooLongMessage =
        $"The payroll export period must not exceed {PayrollExportConstants.MaxPeriodDays} days.";

    private static readonly string InvalidLanguageMessage =
        $"The export language must be a culture name of at most {ExportLogLimits.LanguageMaxLength} characters.";

    private const string EmptyClientSelectionMessage =
        "The person selection must not be empty. Omit it to export every person.";

    private readonly IEnumerable<IPayrollExportFormatter> _formatters;
    private readonly IExportFormatPolicy _exportFormatPolicy;
    private readonly IPayrollCompletenessGate _completenessGate;
    private readonly IPayrollExportDataLoader _dataLoader;
    private readonly IExportLogItemRepository _exportLogItemRepository;
    private readonly IExportLogRepository _exportLogRepository;
    private readonly IPayrollExportConfigRepository _configRepository;
    private readonly IExportFormatOverrideApplier _overrideApplier;
    private readonly IPayrollArtifactStorage _artifactStorage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PayrollPeriodExportService> _logger;

    public PayrollPeriodExportService(
        IEnumerable<IPayrollExportFormatter> formatters,
        IExportFormatPolicy exportFormatPolicy,
        IPayrollCompletenessGate completenessGate,
        IPayrollExportDataLoader dataLoader,
        IExportLogItemRepository exportLogItemRepository,
        IExportLogRepository exportLogRepository,
        IPayrollExportConfigRepository configRepository,
        IExportFormatOverrideApplier overrideApplier,
        IPayrollArtifactStorage artifactStorage,
        IUnitOfWork unitOfWork,
        ILogger<PayrollPeriodExportService> logger)
    {
        _formatters = formatters;
        _exportFormatPolicy = exportFormatPolicy;
        _completenessGate = completenessGate;
        _dataLoader = dataLoader;
        _exportLogItemRepository = exportLogItemRepository;
        _exportLogRepository = exportLogRepository;
        _configRepository = configRepository;
        _overrideApplier = overrideApplier;
        _artifactStorage = artifactStorage;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PayrollExportPreviewDto> PreviewAsync(
        DateOnly from,
        DateOnly until,
        string format,
        IReadOnlyCollection<Guid>? clientIds,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(from, until);
        var scope = NormalizeClientIds(clientIds);
        await ResolveFormatterAsync(format, cancellationToken);

        var selection = await SelectAsync(from, until, format, scope, cancellationToken);
        var changed = selection.Changed;

        return new PayrollExportPreviewDto
        {
            CanExport = selection.Completeness.IsComplete && changed.Count > 0,
            IsComplete = selection.Completeness.IsComplete,
            PersonCount = selection.Decisions.Count,
            AlreadyExportedCount = selection.Decisions.Count - changed.Count,
            NewOrChangedPersons = changed.Select(ToPersonDto).ToList(),
            Blockers = selection.Completeness.Blockers,
            BlockerTotal = selection.Completeness.BlockerTotal
        };
    }

    public async Task<PayrollExportOutcome> ExportAsync(
        DateOnly from,
        DateOnly until,
        string format,
        string language,
        IReadOnlyCollection<Guid>? clientIds,
        string actor,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(from, until);
        ValidateLanguage(language);
        var scope = NormalizeClientIds(clientIds);
        var formatter = await ResolveFormatterAsync(format, cancellationToken);

        var selection = await SelectAsync(from, until, format, scope, cancellationToken);

        if (!selection.Completeness.IsComplete)
        {
            throw new PayrollExportBlockedException(selection.Completeness);
        }

        if (selection.Decisions.Count == 0)
        {
            throw new InvalidRequestException(NoClosedDataMessage);
        }

        var changed = selection.Changed;
        if (changed.Count == 0)
        {
            throw new PayrollExportNothingNewException();
        }

        var isSupplementary = changed.Any(d => d.Previous is not null);

        var config = await _configRepository.GetAsync(cancellationToken);
        var overrideApplied = await _overrideApplier.ApplyAsync(format, config, cancellationToken);

        var data = new PayrollExportData
        {
            StartDate = from,
            EndDate = until,
            Employees = changed.Select(d => d.Employee).ToList()
        };
        var result = formatter.Format(data, config);

        var exportLogId = Guid.NewGuid();
        var fileName = BuildFileName(from, until, isSupplementary, formatter.FileExtension);
        var storageKey = BuildStorageKey(from, until, exportLogId, formatter.FileExtension);

        var exportLog = new ExportLog
        {
            Id = exportLogId,
            Format = format,
            StartDate = from,
            EndDate = until,
            GroupId = null,
            Language = language,
            FileName = fileName,
            FileSize = result.Content.LongLength,
            RecordCount = result.RecordCount,
            ExportedAt = DateTime.UtcNow,
            ExportedBy = actor,
            OverrideApplied = overrideApplied,
            PersonCount = changed.Count,
            IsSupplementary = isSupplementary,
            StorageKey = storageKey
        };
        exportLog.RecordSkips(result);

        var items = changed.Select(d => ToItem(d, exportLogId, from, until, format)).ToList();

        await _artifactStorage.UploadAsync(storageKey, result.Content, cancellationToken);

        try
        {
            await _exportLogRepository.AddAsync(exportLog, cancellationToken);
            await _exportLogItemRepository.AddRangeAsync(items, cancellationToken);
            await _unitOfWork.CompleteAsync();
        }
        catch (Exception ex)
        {
            await DeleteOrphanedArtifactAsync(storageKey, ex);

            if (ex is DatabaseUpdateException { IsDuplicate: true })
            {
                throw new PayrollExportConcurrentException();
            }

            throw;
        }

        LogRun(exportLog, result);

        return new PayrollExportOutcome
        {
            FileContent = result.Content,
            FileName = fileName,
            ContentType = formatter.ContentType,
            SkippedEntryCount = result.TotalSkippedCount,
            AbsenceMappingInvalid = result.AbsenceMappingInvalid,
            PersonCount = changed.Count,
            IsSupplementary = isSupplementary,
            ExportLogId = exportLogId
        };
    }

    private static void ValidatePeriod(DateOnly from, DateOnly until)
    {
        if (from > until)
        {
            throw new InvalidRequestException(FromAfterUntilMessage);
        }

        if (until.DayNumber - from.DayNumber + 1 > PayrollExportConstants.MaxPeriodDays)
        {
            throw new InvalidRequestException(PeriodTooLongMessage);
        }
    }

    private static void ValidateLanguage(string language)
    {
        if (string.IsNullOrWhiteSpace(language) || language.Length > ExportLogLimits.LanguageMaxLength)
        {
            throw new InvalidRequestException(InvalidLanguageMessage);
        }
    }

    private async Task<IPayrollExportFormatter> ResolveFormatterAsync(string format, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            throw new InvalidRequestException("Export format must be specified.");
        }

        var formatter = _formatters.FirstOrDefault(f => f.FormatKey == format)
            ?? throw new InvalidRequestException($"Unknown payroll export format: {format}");

        if (!await _exportFormatPolicy.IsEnabledAsync(format, cancellationToken))
        {
            throw new InvalidRequestException($"Payroll export format is not enabled: {format}");
        }

        return formatter;
    }

    private static IReadOnlyCollection<Guid>? NormalizeClientIds(IReadOnlyCollection<Guid>? clientIds)
    {
        if (clientIds is null)
        {
            return null;
        }

        var distinct = clientIds.Distinct().ToList();
        if (distinct.Count == 0)
        {
            throw new InvalidRequestException(EmptyClientSelectionMessage);
        }

        return distinct;
    }

    private async Task<ExportSelection> SelectAsync(
        DateOnly from,
        DateOnly until,
        string format,
        IReadOnlyCollection<Guid>? clientIds,
        CancellationToken cancellationToken)
    {
        var completeness = await _completenessGate.CheckAsync(from, until, clientIds, cancellationToken);
        var loaded = await _dataLoader.LoadAsync(from, until, clientIds, cancellationToken);
        var latest = await _exportLogItemRepository.GetLatestItemsAsync(from, until, format, clientIds, cancellationToken);

        var decisions = loaded.Employees
            .Where(e => e.Entries.Count > 0)
            .Select(e => new PersonDecision(
                e,
                PayrollPersonContentHash.Compute(e.Entries),
                latest.GetValueOrDefault(e.ClientId)))
            .ToList();

        return new ExportSelection(completeness, decisions);
    }

    private static PayrollExportPersonDto ToPersonDto(PersonDecision decision)
    {
        return new PayrollExportPersonDto
        {
            ClientId = decision.Employee.ClientId,
            ClientName = decision.Employee.FullName,
            IdNumber = decision.Employee.IdNumber,
            IsNew = decision.Previous is null,
            PreviousRevision = decision.Previous?.Revision
        };
    }

    private static ExportLogItem ToItem(
        PersonDecision decision, Guid exportLogId, DateOnly from, DateOnly until, string format)
    {
        return new ExportLogItem
        {
            ExportLogId = exportLogId,
            ClientId = decision.Employee.ClientId,
            StartDate = from,
            EndDate = until,
            Format = format,
            Revision = (decision.Previous?.Revision ?? 0) + 1,
            ContentHash = decision.Hash,
            EntryCount = decision.Employee.Entries.Count,
            EntriesJson = PayrollEntriesSnapshot.ToJson(decision.Employee.Entries),
            IsSupplementary = decision.Previous is not null
        };
    }

    private static string BuildFileName(DateOnly from, DateOnly until, bool isSupplementary, string extension)
    {
        var suffix = isSupplementary ? PayrollExportConstants.SupplementaryFileNameSuffix : string.Empty;
        var start = from.ToString(PayrollExportConstants.FileNameDateFormat, CultureInfo.InvariantCulture);
        var end = until.ToString(PayrollExportConstants.FileNameDateFormat, CultureInfo.InvariantCulture);
        return $"{PayrollExportConstants.FileNamePrefix}_{start}_{end}{suffix}{extension}";
    }

    private static string BuildStorageKey(DateOnly from, DateOnly until, Guid exportLogId, string extension)
    {
        var start = from.ToString(PayrollExportConstants.StorageKeyDateFormat, CultureInfo.InvariantCulture);
        var end = until.ToString(PayrollExportConstants.StorageKeyDateFormat, CultureInfo.InvariantCulture);
        return $"{PayrollExportConstants.StorageKeyPrefix}/{start}-{end}/{exportLogId}{extension}";
    }

    private async Task DeleteOrphanedArtifactAsync(string storageKey, Exception commitFailure)
    {
        _logger.LogWarning(
            commitFailure,
            "Payroll export: committing the export log failed; the uploaded artifact '{StorageKey}' is removed again.",
            storageKey);

        try
        {
            await _artifactStorage.DeleteAsync(storageKey, CancellationToken.None);
        }
        catch (Exception deleteFailure)
        {
            _logger.LogError(
                deleteFailure,
                "Payroll export: the orphaned artifact '{StorageKey}' could not be removed. No export log references it.",
                storageKey);
        }
    }

    private void LogRun(ExportLog exportLog, PayrollExportResult result)
    {
        _logger.LogInformation(
            "Payroll export {ExportLogId}: {PersonCount} person(s), period {Start}..{End}, format '{Format}', supplementary {IsSupplementary}, {RecordCount} lines.",
            exportLog.Id,
            exportLog.PersonCount,
            exportLog.StartDate,
            exportLog.EndDate,
            exportLog.Format,
            exportLog.IsSupplementary,
            exportLog.RecordCount);

        if (result.TotalSkippedCount > 0 || result.AbsenceMappingInvalid)
        {
            _logger.LogWarning(
                "Payroll export {ExportLogId}: {Skipped} entries were not written; absence mapping invalid: {MappingInvalid}.",
                exportLog.Id,
                result.TotalSkippedCount,
                result.AbsenceMappingInvalid);
        }
    }

    private sealed record PersonDecision(PayrollEmployee Employee, string Hash, LatestExportedItem? Previous)
    {
        public bool IsChanged => Previous is null || !string.Equals(Previous.ContentHash, Hash, StringComparison.Ordinal);
    }

    private sealed class ExportSelection
    {
        public ExportSelection(PayrollCompletenessResult completeness, List<PersonDecision> decisions)
        {
            Completeness = completeness;
            Decisions = decisions;
            Changed = decisions.Where(d => d.IsChanged).ToList();
        }

        public PayrollCompletenessResult Completeness { get; }

        public List<PersonDecision> Decisions { get; }

        public List<PersonDecision> Changed { get; }
    }
}
