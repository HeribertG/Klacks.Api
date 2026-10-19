// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Scheduled ERP order import over the erp_import_file ledger. On every due run (cron occurrence claimed
/// through a conditional settings update) it guarantees the single default drop point exists -- via the
/// same get-or-create provider the settings/status skills use, so a fresh install never silently ignores
/// files -- then (0) migrates files the pre-ledger import left in the processing/processed/error
/// sub-folders of every drop point (enabled or not, so the table-only listing shows them) into the ledger
/// and back into the inbox (ErpImportLegacyFolderMigrator, idempotent), (1) flips Importing rows whose
/// heartbeat is older than ErpImportFileTiming.StaleAfter to Interrupted (global, once per run), (1b) removes
/// expired files of every drop point (ErpImportRetentionService: Completed/CompletedWithWarnings files and
/// soft-deleted rows older than ErpImportFileTiming.RetentionPeriod, bounded per run; Failed/Interrupted rows are
/// never removed), (2) registers new inbox files of every enabled drop point as Received rows, before anything else is staged
/// and outside every per-order transaction, and (3) hands
/// each Received row of every enabled drop point, oldest first, to ErpImportFileImporter, which claims it
/// through a conditional ledger update (multi-instance safe; a lost claim skips the file), imports it and
/// finishes the row. Files are never moved by the import: the raw XML stays at its inbox key. Inbox files get
/// their last write time as ReceivedAt, so "oldest first" follows the delivery order, not the random key order.
/// Migration, retention, registration and import are each guarded per drop point: an unexpected error is logged, the
/// scope's tracked changes are discarded and the run continues with the next step or drop point.
/// Cancellation stops the run between files and orders; an interrupted row ages into Interrupted.
/// </summary>
/// <param name="legacyFolderMigrator">Moves files of the pre-ledger sub-folders into the ledger and the inbox</param>
/// <param name="fileRepository">Ledger repository: stale-row reaper and the Received rows of a drop point</param>
/// <param name="retentionService">Removes expired files and soft-deleted rows of a drop point</param>
/// <param name="inboxRegistrationService">Registers files dropped directly into an inbox folder</param>
/// <param name="fileImporter">Claims, imports and finishes one Received row</param>
/// <param name="timeProvider">Clock for the stale-heartbeat cutoff</param>
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Assistant.Scheduling;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Imports;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Imports;
using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Application.Services.Imports;

public class ErpOrderImportRunner : IErpOrderImportRunner
{
    private static readonly TimeSpan CatchUpWindow = TimeSpan.FromHours(1);

    private readonly IErpDropPointRepository _dropPointRepository;
    private readonly IErpDefaultDropPointProvider _defaultDropPointProvider;
    private readonly ErpImportLegacyFolderMigrator _legacyFolderMigrator;
    private readonly IErpImportFileRepository _fileRepository;
    private readonly ErpImportRetentionService _retentionService;
    private readonly ErpImportInboxRegistrationService _inboxRegistrationService;
    private readonly ErpImportFileImporter _fileImporter;
    private readonly ISettingsRepository _settingsRepository;
    private readonly ICompanyClock _companyClock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ErpImportRunState _runState;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ErpOrderImportRunner> _logger;
    private readonly ErpCronTimeZoneDriftNotifier _driftNotifier;

    public ErpOrderImportRunner(
        IErpDropPointRepository dropPointRepository,
        IErpDefaultDropPointProvider defaultDropPointProvider,
        ErpImportLegacyFolderMigrator legacyFolderMigrator,
        IErpImportFileRepository fileRepository,
        ErpImportRetentionService retentionService,
        ErpImportInboxRegistrationService inboxRegistrationService,
        ErpImportFileImporter fileImporter,
        ISettingsRepository settingsRepository,
        ICompanyClock companyClock,
        IUnitOfWork unitOfWork,
        ErpImportRunState runState,
        TimeProvider timeProvider,
        ILogger<ErpOrderImportRunner> logger,
        ErpCronTimeZoneDriftNotifier driftNotifier)
    {
        _dropPointRepository = dropPointRepository;
        _defaultDropPointProvider = defaultDropPointProvider;
        _legacyFolderMigrator = legacyFolderMigrator;
        _fileRepository = fileRepository;
        _retentionService = retentionService;
        _inboxRegistrationService = inboxRegistrationService;
        _fileImporter = fileImporter;
        _settingsRepository = settingsRepository;
        _companyClock = companyClock;
        _unitOfWork = unitOfWork;
        _runState = runState;
        _timeProvider = timeProvider;
        _logger = logger;
        _driftNotifier = driftNotifier;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        if (!await IsDueAsync(cancellationToken))
        {
            return;
        }

        _runState.MarkStarted();
        try
        {
            await _defaultDropPointProvider.GetOrCreateDefaultAsync(cancellationToken);

            var allDropPoints = (await _dropPointRepository.List()).ToList();
            foreach (var dropPoint in allDropPoints)
            {
                await MigrateLegacyFoldersAsync(dropPoint, cancellationToken);
            }

            await ReapStaleImportsAsync(cancellationToken);

            await PurgeExpiredFilesAsync(allDropPoints, cancellationToken);

            var dropPoints = allDropPoints.Where(d => d.IsEnabled).ToList();

            foreach (var dropPoint in dropPoints)
            {
                await RegisterInboxFilesAsync(dropPoint, cancellationToken);
            }

            foreach (var dropPoint in dropPoints)
            {
                await ImportReceivedFilesAsync(dropPoint, cancellationToken);
            }
        }
        finally
        {
            _runState.MarkFinished();
        }
    }

    private async Task<bool> IsDueAsync(CancellationToken cancellationToken)
    {
        var cronExpression = (await _settingsRepository.GetSetting(ErpImportSettingsTypes.CronExpression))?.Value
            ?? ErpImportSettingsTypes.DefaultCronExpression;
        var timeZoneId = await ErpImportCronTimeZone.ResolveAsync(_settingsRepository, _companyClock, _logger, _driftNotifier, cancellationToken);
        // Read untracked on purpose: the occurrence is claimed through a conditional update that
        // bypasses the change tracker, so a tracked instance would keep the pre-claim value and any
        // later save in the same scope would silently roll the claim back.
        var nextRunSetting = await _settingsRepository.GetSettingNoTracking(ErpImportSettingsTypes.NextRunUtc);
        var nowUtc = DateTime.UtcNow;

        DateTime? nextRunUtc = DateTime.TryParse(
            nextRunSetting?.Value,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.RoundtripKind,
            out var parsed)
            ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
            : null;

        var nextOccurrence = FormatNextRun(CronSchedule.GetNextOccurrenceUtc(cronExpression, timeZoneId, nowUtc));

        if (nextRunUtc == null)
        {
            await SeedNextRunAsync(nextRunSetting, nextOccurrence);
            return false;
        }

        var decision = new ScheduledTaskDuePolicy().Decide(nextRunUtc, nowUtc, CatchUpWindow);
        if (decision == ScheduledTaskRunDecision.NotDue)
        {
            return false;
        }

        var claimed = await _settingsRepository.TryAdvanceSettingAsync(
            ErpImportSettingsTypes.NextRunUtc,
            nextRunSetting!.Value,
            nextOccurrence);

        if (!claimed)
        {
            _logger.LogInformation("ERP import: this occurrence was already claimed elsewhere, skipping");
            return false;
        }

        return decision == ScheduledTaskRunDecision.Fire;
    }

    private static string FormatNextRun(DateTime? nextRunUtc)
    {
        return nextRunUtc?.ToString("O") ?? string.Empty;
    }

    private async Task SeedNextRunAsync(Settings? existing, string value)
    {
        if (existing != null)
        {
            existing.Value = value;
            await _settingsRepository.PutSetting(existing);
        }
        else
        {
            await _settingsRepository.AddSetting(new Settings { Id = Guid.NewGuid(), Type = ErpImportSettingsTypes.NextRunUtc, Value = value });
        }

        await _unitOfWork.CompleteAsync();
    }

    private async Task MigrateLegacyFoldersAsync(ErpDropPoint dropPoint, CancellationToken cancellationToken)
    {
        try
        {
            await _legacyFolderMigrator.MigrateAsync(dropPoint, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "ERP import: legacy folders of drop point {DropPointId} could not be migrated; the import continues", dropPoint.Id);
        }
    }

    private async Task ReapStaleImportsAsync(CancellationToken cancellationToken)
    {
        var cutoff = _timeProvider.GetUtcNow().UtcDateTime - ErpImportFileTiming.StaleAfter;
        var reaped = await _fileRepository.ReapStaleAsync(cutoff, cancellationToken);
        if (reaped > 0)
        {
            _logger.LogWarning("ERP import: {Count} import(s) without heartbeat since {Cutoff:O} marked as interrupted", reaped, cutoff);
        }
    }

    private async Task PurgeExpiredFilesAsync(IReadOnlyList<ErpDropPoint> dropPoints, CancellationToken cancellationToken)
    {
        var removed = 0;
        foreach (var dropPoint in dropPoints)
        {
            try
            {
                removed += await _retentionService.PurgeExpiredAsync(dropPoint, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                _unitOfWork.DiscardTrackedChanges();
                _logger.LogError(ex, "ERP import: expired files of drop point {DropPointId} could not be removed; the import continues", dropPoint.Id);
            }
        }

        if (removed > 0)
        {
            _logger.LogInformation(
                "ERP import: {Count} file(s) older than {RetentionDays} days removed (processed files and deleted records)",
                removed,
                ErpImportFileTiming.RetentionPeriod.TotalDays);
        }
    }

    private async Task RegisterInboxFilesAsync(ErpDropPoint dropPoint, CancellationToken cancellationToken)
    {
        try
        {
            await _inboxRegistrationService.RegisterInboxFilesAsync(dropPoint, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _unitOfWork.DiscardTrackedChanges();
            _logger.LogError(ex, "ERP import: inbox of drop point {DropPointId} could not be registered; the import continues", dropPoint.Id);
        }
    }

    private async Task ImportReceivedFilesAsync(ErpDropPoint dropPoint, CancellationToken cancellationToken)
    {
        try
        {
            var receivedFiles = await _fileRepository.ListReceivedAsync(dropPoint.Id, cancellationToken);

            foreach (var file in receivedFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _fileImporter.ImportAsync(file, cancellationToken);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _unitOfWork.DiscardTrackedChanges();
            _logger.LogError(ex, "ERP import: files of drop point {DropPointId} could not be imported; the other drop points continue", dropPoint.Id);
        }
    }
}
