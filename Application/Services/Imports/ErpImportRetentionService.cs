// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Retention purge of the erp_import_file ledger for one drop point, run by every import run after the
/// stale-heartbeat reaper and before the inbox registration. Due are (cutoff = now - ErpImportFileTiming.RetentionPeriod):
/// - Completed and CompletedWithWarnings rows that finished before the cutoff (received, when the finish time is
///   missing): the stored object is deleted FIRST (a no-op when it is already gone), then the row is hard-deleted by
///   a conditional delete that re-checks status and age, so a row that changed meanwhile is kept. If the object
///   delete fails, the row stays and the next run tries again -- a row-less object with a registration-shaped key
///   would otherwise come back as LedgerEntryMissing;
/// - soft-deleted rows whose deletion is older than the cutoff (their object was deleted with them): the row is only
///   purged while its storage key holds no object any more. The object is NEVER deleted here -- the key may also
///   belong to a live row -- a leftover is removed by the inbox registration's orphan rule and the row purged by a
///   later run. Purging a row whose object still exists would make the registration import the leftover again.
/// Failed, Interrupted (incl. LedgerEntryMissing), Received and Importing rows are never touched.
/// Every erp_import_exception linked to a purged row (open or resolved) is hard-deleted in the same transaction: they
/// are per-file rejection records that are only read through their file (rejected orders of the listing; retry and
/// delete resolve them by file id), the admin alert carried its own copy when it was raised, and keeping them would
/// leave unreachable rows (link nulled by the foreign key) that grow forever. Their ids are read before the row
/// delete because the foreign key sets the link to null.
/// At most ErpImportFileTiming.RetentionBatchSize rows per call (one indexed query when nothing is due); the rest
/// follows in the next runs. An unexpected error of one row is logged and the next row runs; cancellation stops.
/// </summary>
/// <param name="fileRepository">Ledger repository: due rows and the conditional hard deletes</param>
/// <param name="exceptionRepository">Reads and deletes the exceptions linked to a purged row</param>
/// <param name="objectStorageService">Deletes the stored file of an expired row and checks a deleted row's object is gone</param>
/// <param name="unitOfWork">Runs the row delete and the exception delete in one transaction</param>
/// <param name="timeProvider">Clock for the retention cutoff</param>
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Imports;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Models.Imports;

namespace Klacks.Api.Application.Services.Imports;

public class ErpImportRetentionService
{
    private readonly IErpImportFileRepository _fileRepository;
    private readonly IErpImportExceptionRepository _exceptionRepository;
    private readonly IObjectStorageService _objectStorageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ErpImportRetentionService> _logger;

    public ErpImportRetentionService(
        IErpImportFileRepository fileRepository,
        IErpImportExceptionRepository exceptionRepository,
        IObjectStorageService objectStorageService,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        ILogger<ErpImportRetentionService> logger)
    {
        _fileRepository = fileRepository;
        _exceptionRepository = exceptionRepository;
        _objectStorageService = objectStorageService;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>
    /// Removes the due rows of the drop point (at most ErpImportFileTiming.RetentionBatchSize) and returns how many
    /// rows were removed.
    /// </summary>
    public async Task<int> PurgeExpiredAsync(ErpDropPoint dropPoint, CancellationToken cancellationToken = default)
    {
        var cutoff = _timeProvider.GetUtcNow().UtcDateTime - ErpImportFileTiming.RetentionPeriod;
        var candidates = await _fileRepository.ListExpiredAsync(
            dropPoint.Id, cutoff, ErpImportFileTiming.RetentionBatchSize, cancellationToken);

        var removed = 0;
        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var purged = candidate.IsDeleted
                    ? await TryPurgeDeletedAsync(candidate, cutoff, cancellationToken)
                    : await TryRemoveProcessedAsync(candidate, cutoff, cancellationToken);
                if (purged)
                {
                    removed++;
                }
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "ERP import retention: file {Key} could not be removed; the next run tries again", candidate.StorageKey.ForLog());
            }
        }

        return removed;
    }

    private async Task<bool> TryRemoveProcessedAsync(ErpImportFileRetentionCandidate candidate, DateTime cutoff, CancellationToken cancellationToken)
    {
        try
        {
            await _objectStorageService.DeleteAsync(candidate.StorageKey, cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "ERP import retention: stored file {Key} could not be deleted; its import record is kept and the next run tries again", candidate.StorageKey.ForLog());
            return false;
        }

        var deleted = await DeleteRowWithExceptionsAsync(
            candidate.Id, () => _fileRepository.TryDeleteExpiredProcessedAsync(candidate.Id, cutoff, cancellationToken), cancellationToken);
        if (!deleted)
        {
            _logger.LogWarning("ERP import retention: import record of {Key} changed while its stored file was being deleted; the record is kept", candidate.StorageKey.ForLog());
        }

        return deleted;
    }

    private async Task<bool> TryPurgeDeletedAsync(ErpImportFileRetentionCandidate candidate, DateTime cutoff, CancellationToken cancellationToken)
    {
        if (await _objectStorageService.GetObjectInfoAsync(candidate.StorageKey, cancellationToken) != null)
        {
            _logger.LogInformation("ERP import retention: deleted file {Key} still has a stored object; the record is kept until the object is gone", candidate.StorageKey.ForLog());
            return false;
        }

        return await DeleteRowWithExceptionsAsync(
            candidate.Id, () => _fileRepository.TryPurgeExpiredDeletedAsync(candidate.Id, cutoff, cancellationToken), cancellationToken);
    }

    private async Task<bool> DeleteRowWithExceptionsAsync(Guid fileId, Func<Task<bool>> deleteRow, CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var exceptionIds = await _exceptionRepository.GetIdsByImportFileAsync(fileId, cancellationToken);
            if (!await deleteRow())
            {
                return false;
            }

            await _exceptionRepository.DeleteByIdsAsync(exceptionIds, cancellationToken);
            return true;
        });
    }
}
