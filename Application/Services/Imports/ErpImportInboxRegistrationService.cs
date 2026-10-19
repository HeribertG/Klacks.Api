// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Registers files found directly in a drop point's inbox folder in the erp_import_file ledger (channel
/// InboxFolder, no uploader). Only keys directly below the inbox prefix count; objects in any sub-folder (the
/// legacy processing/processed/error segments) are ignored. Which rows exist is read as a projection (key and
/// deleted flag, soft-deleted rows included), and every key is classified:
/// - a live row exists: already registered, skipped without reading the file;
/// - only a soft-deleted row exists: the file was deleted (or its deletion is still running) -- never registered
///   again; the leftover object is deleted (best effort, logged);
/// - no row, ERP-chosen name: a new delivery. It is hashed (size, SHA-256, last write time) under its own name
///   FIRST, then renamed to a unique "{guid}-{name}" key through the atomic TryMoveAsync (the loser of a concurrent
///   rename skips it) and registered as Received with the object's last write time as ReceivedAt -- the runner
///   imports oldest first, so an older delivery never supersedes a newer one. The rename is needed because files
///   stay in the inbox after their import: an ERP re-delivering "orders.xml" every night would otherwise overwrite
///   a registered key. Hashing before the rename means an unreadable file keeps its plain name and is retried as a
///   new delivery by the next run;
/// - no row, but the name already carries the "{32 hex}-" id Klacks gives every registered file: a stored file
///   that lost its record (database restore or reset, or a registration that crashed after the upload/rename). It
///   is registered as Failed with ErpImportFileFailureReasons.LedgerEntryMissing (hash, size, last write time) and
///   never imported automatically: replaying it blindly could supersede newer or sealed orders with stale data.
///   An admin checks it and retries (the stored hash matches, so the retry re-queues it) or deletes it. Accepted
///   limitation: an ERP whose own file names start with 32 hex characters and a hyphen is treated the same way.
/// Every file is handled on its own: an unexpected error (e.g. an unreadable file) is logged, the scope's tracked
/// changes are discarded and the next file runs; only cancellation stops the loop. Each row is committed on its
/// own. ListAsync already hides objects inside the write-stability window, so half-written files are neither
/// renamed nor registered.
/// </summary>
/// <param name="objectStorageService">Lists the inbox, hashes new objects, renames ERP-named files and deletes leftovers</param>
/// <param name="repository">Ledger repository: key/deleted projection of the rows of the listed keys</param>
/// <param name="registrar">Writes and commits the row, detaching it on a concurrent registration</param>
/// <param name="unitOfWork">Discards what a failed file left tracked before the next file runs</param>
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Imports;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Models.Imports;
using Klacks.Api.Domain.Services.Imports;

namespace Klacks.Api.Application.Services.Imports;

public class ErpImportInboxRegistrationService
{
    private readonly IObjectStorageService _objectStorageService;
    private readonly IErpImportFileRepository _repository;
    private readonly ErpImportFileRegistrar _registrar;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ErpImportInboxRegistrationService> _logger;

    public ErpImportInboxRegistrationService(
        IObjectStorageService objectStorageService,
        IErpImportFileRepository repository,
        ErpImportFileRegistrar registrar,
        IUnitOfWork unitOfWork,
        ILogger<ErpImportInboxRegistrationService> logger)
    {
        _objectStorageService = objectStorageService;
        _repository = repository;
        _registrar = registrar;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<int> RegisterInboxFilesAsync(ErpDropPoint dropPoint, CancellationToken cancellationToken = default)
    {
        var prefix = ErpImportStorageKeys.NormalizePrefix(dropPoint.BucketPrefix);
        var inboxKeys = (await _objectStorageService.ListAsync(prefix, cancellationToken))
            .Where(key => IsDirectlyInInbox(key, prefix))
            .ToList();

        if (inboxKeys.Count == 0)
        {
            return 0;
        }

        var keyStates = await _repository.GetKeyStatesAsync(inboxKeys, cancellationToken);
        var liveKeys = keyStates.Where(state => !state.IsDeleted).Select(state => state.StorageKey).ToHashSet(StringComparer.Ordinal);
        var deletedKeys = keyStates.Where(state => state.IsDeleted).Select(state => state.StorageKey).ToHashSet(StringComparer.Ordinal);

        var registered = 0;
        foreach (var key in inboxKeys.Where(key => !liveKeys.Contains(key)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (deletedKeys.Contains(key))
                {
                    await RemoveLeftoverOfDeletedFileAsync(key, cancellationToken);
                    continue;
                }

                if (await TryRegisterAsync(dropPoint, prefix, key, cancellationToken))
                {
                    registered++;
                }
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "ERP import: inbox file {Key} could not be registered; the next run tries again", key.ForLog());
                _unitOfWork.DiscardTrackedChanges();
            }
        }

        return registered;
    }

    private async Task RemoveLeftoverOfDeletedFileAsync(string key, CancellationToken cancellationToken)
    {
        _logger.LogWarning("ERP import: inbox file {Key} belongs to a deleted import file; deleting the leftover object instead of registering it", key.ForLog());
        await _objectStorageService.DeleteAsync(key, cancellationToken);
    }

    private async Task<bool> TryRegisterAsync(ErpDropPoint dropPoint, string prefix, string key, CancellationToken cancellationToken)
    {
        var storedFileName = key[prefix.Length..];
        var hasRegistrationShape = ErpImportStorageKeys.HasUploadIdPrefix(storedFileName);
        var storageKey = hasRegistrationShape
            ? key
            : ErpImportStorageKeys.BuildUploadKey(dropPoint.BucketPrefix, storedFileName);

        if (storageKey.Length > ErpImportFileLimits.StorageKeyMaxLength)
        {
            _logger.LogWarning(
                "ERP import: inbox file {Key} would get a key longer than {MaxLength} characters, skipping",
                key.ForLog(),
                ErpImportFileLimits.StorageKeyMaxLength);
            return false;
        }

        var storedObject = await _objectStorageService.GetObjectInfoAsync(key, cancellationToken);
        if (storedObject == null)
        {
            _logger.LogInformation("ERP import: inbox file {Key} disappeared before it could be registered, skipping", key.ForLog());
            return false;
        }

        if (hasRegistrationShape)
        {
            _logger.LogWarning(
                "ERP import: inbox file {Key} carries a registration id but has no import record (e.g. after a database restore); registered as failed, it is not imported automatically",
                key.ForLog());
            return await RegisterAsync(dropPoint, key, storedFileName, storedObject, ErpImportFileStatus.Failed, ErpImportFileFailureReasons.LedgerEntryMissing, cancellationToken);
        }

        if (!await _objectStorageService.TryMoveAsync(key, storageKey, cancellationToken))
        {
            _logger.LogInformation("ERP import: inbox file {Key} was taken over by another caller, skipping", key.ForLog());
            return false;
        }

        return await RegisterAsync(dropPoint, storageKey, storedFileName, storedObject, ErpImportFileStatus.Received, failureReason: null, cancellationToken);
    }

    private async Task<bool> RegisterAsync(
        ErpDropPoint dropPoint,
        string storageKey,
        string storedFileName,
        StoredObjectInfo storedObject,
        ErpImportFileStatus status,
        string? failureReason,
        CancellationToken cancellationToken)
    {
        var registration = new ErpImportFileRegistration(
            dropPoint,
            storageKey,
            ErpImportStorageKeys.ToDisplayFileName(storedFileName),
            storedObject,
            ErpImportChannel.InboxFolder,
            UploadedBy: null,
            ReceivedAt: storedObject.LastModifiedUtc,
            Status: status,
            FailureReason: failureReason);

        return await _registrar.RegisterAsync(registration, cancellationToken) != null;
    }

    private static bool IsDirectlyInInbox(string key, string prefix)
    {
        return key.Length > prefix.Length
            && key.StartsWith(prefix, StringComparison.Ordinal)
            && key.IndexOf(ErpImportStorageKeys.KeySeparator, prefix.Length) < 0;
    }
}
