// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Migrates the files the pre-ledger import left in a drop point's sub-folders into the erp_import_file
/// ledger and moves them back into the inbox, where every file lives now. Runs at the start of every import
/// run, before the inbox registration; with empty or absent sub-folders it only lists the drop point.
/// Mapping: processed/ -> Completed, error/ -> Failed (reason = the latest exception recorded for the file's
/// original inbox key, or LegacyErrorWithoutReason), processing/ -> Interrupted (LegacyImportInterrupted).
/// The counters of a migrated file are unknown: TotalOrders stays null and the others 0. Received and
/// finish time are the object's last write time, the channel is InboxFolder. Exceptions recorded for the
/// original inbox key are linked to the row (ImportFileId). For a Completed row (processed/: the old import
/// finished that file without a single rejection) the linked open exceptions are stale -- left over from an
/// earlier delivery under the same key, which the old retry/delete never resolved -- and are resolved at once
/// through the same mechanism retry and delete use; Failed and Interrupted rows keep them open.
/// Target key: a stored name that already carries an upload id ("{32 hex}-name", an upload) goes back to its
/// original inbox key; an ERP-named file gets "{id}-name" with an id derived from the legacy key, so a rerun
/// computes the same key. When a row already exists for the target key -- registered by an upload before the
/// old import moved its file away -- that row is updated instead of inserting a second one.
/// Order per file: the row is written (and committed) BEFORE the object is moved, so a moved object always
/// has its row and the inbox registration never sees it as a new Received file. A crash between the two
/// steps is healed by the next run: it finds the row by the deterministic target key and only redoes the
/// move. When the target key is already occupied by another object, a second key derived from the legacy key
/// (with its own id) is used instead -- also deterministic, so a failed move never multiplies rows; when both
/// are occupied the file is left in place and logged. Every file is handled on its own: a failure is logged,
/// the scope's tracked changes are discarded and the next file runs.
/// Only files directly inside one of the three sub-folders are migrated; deeper keys are ignored.
/// </summary>
/// <param name="objectStorageService">Lists the sub-folders (with write times), hashes and moves the files</param>
/// <param name="fileRepository">Finds existing rows by key, stages new rows and updates existing ones</param>
/// <param name="exceptionRepository">Supplies the failure reason of error/ files and links their exceptions</param>
/// <param name="unitOfWork">Commits each new row on its own</param>
/// <param name="timeProvider">Resolution time of the stale exceptions of a Completed row</param>
using System.Security.Cryptography;
using System.Text;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Imports;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Models.Imports;
using Klacks.Api.Domain.Services.Imports;

namespace Klacks.Api.Application.Services.Imports;

public class ErpImportLegacyFolderMigrator
{
    private const int LegacyUploadIdLength = 32;
    private const char UploadIdSeparator = '-';
    private const string CollisionKeySalt = "#collision";

    private static readonly (string Folder, ErpImportFileStatus Status)[] LegacyFolders =
    [
        (ErpImportLegacyFolders.Processed, ErpImportFileStatus.Completed),
        (ErpImportLegacyFolders.Error, ErpImportFileStatus.Failed),
        (ErpImportLegacyFolders.Processing, ErpImportFileStatus.Interrupted)
    ];

    private readonly IObjectStorageService _objectStorageService;
    private readonly IErpImportFileRepository _fileRepository;
    private readonly IErpImportExceptionRepository _exceptionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ErpImportLegacyFolderMigrator> _logger;

    public ErpImportLegacyFolderMigrator(
        IObjectStorageService objectStorageService,
        IErpImportFileRepository fileRepository,
        IErpImportExceptionRepository exceptionRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        ILogger<ErpImportLegacyFolderMigrator> logger)
    {
        _objectStorageService = objectStorageService;
        _fileRepository = fileRepository;
        _exceptionRepository = exceptionRepository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<int> MigrateAsync(ErpDropPoint dropPoint, CancellationToken cancellationToken = default)
    {
        var prefix = ErpImportStorageKeys.NormalizePrefix(dropPoint.BucketPrefix);
        var legacyFiles = (await _objectStorageService.ListWithMetadataAsync(prefix, cancellationToken))
            .Select(storedObject => ToLegacyFile(prefix, storedObject))
            .OfType<LegacyFile>()
            .ToList();

        var migrated = 0;
        foreach (var legacyFile in legacyFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (await MigrateFileAsync(dropPoint, prefix, legacyFile, cancellationToken))
                {
                    migrated++;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                _unitOfWork.DiscardTrackedChanges();
                _logger.LogError(ex, "ERP import: legacy file {Key} could not be migrated; the next run tries again", legacyFile.Key.ForLog());
            }
        }

        if (migrated > 0)
        {
            _logger.LogInformation("ERP import: {Count} file(s) of the pre-ledger sub-folders migrated into the ledger", migrated);
        }

        return migrated;
    }

    private async Task<bool> MigrateFileAsync(ErpDropPoint dropPoint, string prefix, LegacyFile legacyFile, CancellationToken cancellationToken)
    {
        var targetKey = await ChooseFreeTargetKeyAsync(prefix, legacyFile, cancellationToken);
        if (targetKey == null)
        {
            _logger.LogWarning(
                "ERP import: legacy file {Key} cannot be moved back into the inbox, both target keys are occupied",
                legacyFile.Key.ForLog());
            return false;
        }

        if (targetKey.Length > ErpImportFileLimits.StorageKeyMaxLength)
        {
            _logger.LogWarning(
                "ERP import: legacy file {Key} would get a key longer than {MaxLength} characters, not migrated",
                legacyFile.Key.ForLog(),
                ErpImportFileLimits.StorageKeyMaxLength);
            return false;
        }

        var failureReason = await ResolveFailureReasonAsync(prefix, legacyFile, cancellationToken);

        var fileId = await UpsertRowAsync(dropPoint, targetKey, legacyFile, failureReason, cancellationToken);
        if (fileId == null)
        {
            return false;
        }

        await _exceptionRepository.LinkToImportFileAsync(prefix + legacyFile.StoredName, fileId.Value, cancellationToken);
        if (legacyFile.Status == ErpImportFileStatus.Completed)
        {
            await _exceptionRepository.ResolveOpenForImportFileAsync(fileId.Value, _timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        }

        if (!await _objectStorageService.TryMoveAsync(legacyFile.Key, targetKey, cancellationToken))
        {
            _logger.LogWarning(
                "ERP import: legacy file {Key} could not be moved to {TargetKey}; the next run tries again",
                legacyFile.Key.ForLog(),
                targetKey.ForLog());
            return false;
        }

        return true;
    }

    private async Task<string?> ResolveFailureReasonAsync(string prefix, LegacyFile legacyFile, CancellationToken cancellationToken)
    {
        return legacyFile.Status switch
        {
            ErpImportFileStatus.Interrupted => ErpImportFileFailureReasons.LegacyImportInterrupted,
            ErpImportFileStatus.Failed => (await _exceptionRepository.GetByFileKeysAsync([prefix + legacyFile.StoredName], cancellationToken))
                .FirstOrDefault()?.Reason ?? ErpImportFileFailureReasons.LegacyErrorWithoutReason,
            _ => null
        };
    }

    private async Task<Guid?> UpsertRowAsync(ErpDropPoint dropPoint, string targetKey, LegacyFile legacyFile, string? failureReason, CancellationToken cancellationToken)
    {
        var existing = (await _fileRepository.GetByStorageKeysAsync([targetKey], cancellationToken)).FirstOrDefault();
        if (existing == null)
        {
            return await InsertRowAsync(dropPoint, targetKey, legacyFile, failureReason, cancellationToken);
        }

        if (!await _fileRepository.TryApplyLegacyStateAsync(existing.Id, targetKey, legacyFile.Status, failureReason, legacyFile.LastModifiedUtc, cancellationToken))
        {
            _logger.LogInformation(
                "ERP import: legacy file {Key} belongs to a file that is being imported right now; the next run migrates it",
                legacyFile.Key.ForLog());
            return null;
        }

        return existing.Id;
    }

    private async Task<Guid?> InsertRowAsync(
        ErpDropPoint dropPoint,
        string targetKey,
        LegacyFile legacyFile,
        string? failureReason,
        CancellationToken cancellationToken)
    {
        var storedObject = await _objectStorageService.GetObjectInfoAsync(legacyFile.Key, cancellationToken);
        if (storedObject == null)
        {
            _logger.LogInformation("ERP import: legacy file {Key} disappeared before it could be migrated", legacyFile.Key.ForLog());
            return null;
        }

        var duplicate = await _fileRepository.FindDuplicateAsync(dropPoint.Id, storedObject.Sha256, excludeId: null, cancellationToken);
        var row = new ErpImportFile
        {
            Id = Guid.NewGuid(),
            DropPointId = dropPoint.Id,
            SourceSystemId = dropPoint.SourceSystemId,
            StorageKey = targetKey,
            FileName = Clamp(ErpImportStorageKeys.ToDisplayFileName(legacyFile.StoredName), ErpImportFileLimits.FileNameMaxLength),
            SizeBytes = storedObject.SizeBytes,
            Sha256 = storedObject.Sha256,
            Channel = ErpImportChannel.InboxFolder,
            Status = legacyFile.Status,
            ReceivedAt = legacyFile.LastModifiedUtc,
            FinishedAt = legacyFile.LastModifiedUtc,
            FailureReason = failureReason == null ? null : Clamp(failureReason, ErpImportFileLimits.FailureReasonMaxLength),
            DuplicateOfFileId = duplicate?.Id
        };

        _fileRepository.Add(row);

        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (DatabaseUpdateException ex) when (ex.IsDuplicate)
        {
            _fileRepository.Detach(row);
            _logger.LogInformation("ERP import: legacy file {Key} is being migrated by another caller, skipping", legacyFile.Key.ForLog());
            return null;
        }

        return row.Id;
    }

    private static LegacyFile? ToLegacyFile(string prefix, StorageObjectMetadata storedObject)
    {
        foreach (var (folder, status) in LegacyFolders)
        {
            var folderPrefix = prefix + folder + ErpImportStorageKeys.KeySeparator;
            if (!storedObject.Key.StartsWith(folderPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            var storedName = storedObject.Key[folderPrefix.Length..];
            return storedName.Length == 0 || storedName.Contains(ErpImportStorageKeys.KeySeparator)
                ? null
                : new LegacyFile(storedObject.Key, storedName, status, DateTime.SpecifyKind(storedObject.LastModifiedUtc, DateTimeKind.Utc));
        }

        return null;
    }

    private async Task<string?> ChooseFreeTargetKeyAsync(string prefix, LegacyFile legacyFile, CancellationToken cancellationToken)
    {
        var primaryKey = ErpImportStorageKeys.HasUploadIdPrefix(legacyFile.StoredName)
            ? prefix + legacyFile.StoredName
            : prefix + DeriveUploadId(legacyFile.Key) + UploadIdSeparator + legacyFile.StoredName;
        var collisionKey = prefix + DeriveUploadId(legacyFile.Key + CollisionKeySalt) + UploadIdSeparator
            + ErpImportStorageKeys.ToDisplayFileName(legacyFile.StoredName);

        foreach (var candidate in new[] { primaryKey, collisionKey })
        {
            if (await _objectStorageService.GetObjectInfoAsync(candidate, cancellationToken) == null)
            {
                return candidate;
            }
        }

        return null;
    }

    private static string DeriveUploadId(string value)
    {
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..LegacyUploadIdLength];
    }

    private static string Clamp(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private sealed record LegacyFile(string Key, string StoredName, ErpImportFileStatus Status, DateTime LastModifiedUtc);
}
