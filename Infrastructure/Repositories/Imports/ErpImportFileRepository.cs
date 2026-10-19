// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// EF-backed erp_import_file ledger. Reads are AsNoTracking and every state transition is a conditional
/// ExecuteUpdateAsync whose winner is decided by the affected row count (pattern of
/// ScheduledTaskRepository.TryClaimAsync), so a ledger row is never flushed by another repository's
/// SaveChangesAsync inside a per-order transaction. Only Add stages through the change tracker. Heartbeat
/// and finish are guarded by instance AND attempt number. The retention purge hard-deletes through conditional
/// ExecuteDeleteAsync calls that re-check status and age, so a row that changed meanwhile is kept.
/// </summary>
/// <param name="context">Shared scoped database context</param>
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Imports;
using Klacks.Api.Domain.Models.Imports;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Imports;

public class ErpImportFileRepository : IErpImportFileRepository
{
    private const int SingleRow = 1;

    private static readonly ErpImportFileStatus[] ProcessedStatuses =
    [
        ErpImportFileStatus.Completed,
        ErpImportFileStatus.CompletedWithWarnings
    ];

    private static readonly ErpImportFileStatus[] RetryableStatuses =
    [
        ErpImportFileStatus.Failed,
        ErpImportFileStatus.Interrupted
    ];

    private static readonly ErpImportFileStatus[] FinishStatuses =
    [
        ErpImportFileStatus.Completed,
        ErpImportFileStatus.CompletedWithWarnings,
        ErpImportFileStatus.Failed,
        ErpImportFileStatus.Interrupted
    ];

    private readonly DataBaseContext _context;

    public ErpImportFileRepository(DataBaseContext context)
    {
        _context = context;
    }

    public void Add(ErpImportFile file)
    {
        _context.ErpImportFiles.Add(file);
    }

    public void Detach(ErpImportFile file)
    {
        _context.Entry(file).State = EntityState.Detached;
    }

    public async Task<List<ErpImportFile>> GetByStorageKeysAsync(IReadOnlyCollection<string> storageKeys, CancellationToken cancellationToken = default)
    {
        if (storageKeys.Count == 0)
        {
            return [];
        }

        return await _context.ErpImportFiles
            .AsNoTracking()
            .Where(f => storageKeys.Contains(f.StorageKey))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ErpImportFileKeyState>> GetKeyStatesAsync(IReadOnlyCollection<string> storageKeys, CancellationToken cancellationToken = default)
    {
        if (storageKeys.Count == 0)
        {
            return [];
        }

        return await _context.ErpImportFiles
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(f => storageKeys.Contains(f.StorageKey))
            .Select(f => new ErpImportFileKeyState(f.StorageKey, f.IsDeleted))
            .ToListAsync(cancellationToken);
    }

    public async Task<ErpImportFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ErpImportFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public async Task<List<ErpImportFile>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        return await _context.ErpImportFiles
            .AsNoTracking()
            .Where(f => ids.Contains(f.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<ErpImportFileStatusCounts> CountByStatusAsync(Guid dropPointId, DateTime staleCutoff, CancellationToken cancellationToken = default)
    {
        var groups = await _context.ErpImportFiles
            .AsNoTracking()
            .Where(f => f.DropPointId == dropPointId)
            .GroupBy(f => new
            {
                f.Status,
                Stale = f.Status == ErpImportFileStatus.Importing && (f.HeartbeatAt ?? f.StartedAt) < staleCutoff
            })
            .Select(g => new { g.Key.Status, g.Key.Stale, Count = g.Count() })
            .ToListAsync(cancellationToken);

        int Sum(Func<ErpImportFileStatus, bool, bool> predicate) =>
            groups.Where(g => predicate(g.Status, g.Stale)).Sum(g => g.Count);

        return new ErpImportFileStatusCounts(
            Received: Sum((status, _) => status == ErpImportFileStatus.Received),
            Importing: Sum((status, stale) => status == ErpImportFileStatus.Importing && !stale),
            Processed: Sum((status, _) => ProcessedStatuses.Contains(status)),
            Failed: Sum((status, stale) => RetryableStatuses.Contains(status) || (status == ErpImportFileStatus.Importing && stale)));
    }

    public async Task<ErpImportFile?> GetLatestFinishedAsync(Guid dropPointId, CancellationToken cancellationToken = default)
    {
        return await _context.ErpImportFiles
            .AsNoTracking()
            .Where(f => f.DropPointId == dropPointId && FinishStatuses.Contains(f.Status) && f.FinishedAt != null)
            .OrderByDescending(f => f.FinishedAt)
            .ThenByDescending(f => f.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ErpImportFile?> FindDuplicateAsync(Guid dropPointId, string sha256, Guid? excludeId, CancellationToken cancellationToken = default)
    {
        return await _context.ErpImportFiles
            .AsNoTracking()
            .Where(f => f.DropPointId == dropPointId && f.Sha256 == sha256 && (excludeId == null || f.Id != excludeId))
            .OrderBy(f => f.ReceivedAt)
            .ThenBy(f => f.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<ErpImportFile>> ListForDropPointAsync(Guid dropPointId, int processedCap, CancellationToken cancellationToken = default)
    {
        var open = await _context.ErpImportFiles
            .AsNoTracking()
            .Where(f => f.DropPointId == dropPointId && !ProcessedStatuses.Contains(f.Status))
            .ToListAsync(cancellationToken);

        var processed = await _context.ErpImportFiles
            .AsNoTracking()
            .Where(f => f.DropPointId == dropPointId && ProcessedStatuses.Contains(f.Status))
            .OrderByDescending(f => f.ReceivedAt)
            .ThenByDescending(f => f.Id)
            .Take(processedCap)
            .ToListAsync(cancellationToken);

        return open
            .Concat(processed)
            .OrderByDescending(f => f.ReceivedAt)
            .ThenByDescending(f => f.Id)
            .ToList();
    }

    public async Task<List<ErpImportFile>> ListReceivedAsync(Guid dropPointId, CancellationToken cancellationToken = default)
    {
        return await _context.ErpImportFiles
            .AsNoTracking()
            .Where(f => f.DropPointId == dropPointId && f.Status == ErpImportFileStatus.Received)
            .OrderBy(f => f.ReceivedAt)
            .ThenBy(f => f.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> TryClaimAsync(Guid id, int expectedAttempt, string instanceId, DateTime now, CancellationToken cancellationToken = default)
    {
        var claimedAttempt = expectedAttempt + 1;
        var affected = await _context.ErpImportFiles
            .Where(f => f.Id == id && f.Status == ErpImportFileStatus.Received && f.Attempt == expectedAttempt)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(f => f.Status, ErpImportFileStatus.Importing)
                    .SetProperty(f => f.StartedAt, now)
                    .SetProperty(f => f.HeartbeatAt, now)
                    .SetProperty(f => f.FinishedAt, (DateTime?)null)
                    .SetProperty(f => f.ClaimedBy, instanceId)
                    .SetProperty(f => f.Attempt, claimedAttempt)
                    .SetProperty(f => f.FailureReason, (string?)null)
                    .SetProperty(f => f.TotalOrders, (int?)null)
                    .SetProperty(f => f.ProcessedOrders, 0)
                    .SetProperty(f => f.NewCount, 0)
                    .SetProperty(f => f.UpdatedCount, 0)
                    .SetProperty(f => f.UnchangedCount, 0)
                    .SetProperty(f => f.SupersededCount, 0)
                    .SetProperty(f => f.RejectedCount, 0),
                cancellationToken);

        return affected == SingleRow;
    }

    public async Task<bool> HeartbeatAsync(Guid id, string instanceId, int attempt, ErpImportFileProgress progress, DateTime now, CancellationToken cancellationToken = default)
    {
        var affected = await OwnedImporting(id, instanceId, attempt)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(f => f.HeartbeatAt, now)
                    .SetProperty(f => f.TotalOrders, progress.TotalOrders)
                    .SetProperty(f => f.ProcessedOrders, progress.ProcessedOrders)
                    .SetProperty(f => f.NewCount, progress.NewCount)
                    .SetProperty(f => f.UpdatedCount, progress.UpdatedCount)
                    .SetProperty(f => f.UnchangedCount, progress.UnchangedCount)
                    .SetProperty(f => f.SupersededCount, progress.SupersededCount)
                    .SetProperty(f => f.RejectedCount, progress.RejectedCount),
                cancellationToken);

        return affected == SingleRow;
    }

    public async Task<bool> FinishAsync(Guid id, string instanceId, int attempt, ErpImportFileStatus terminalStatus, ErpImportFileProgress progress, string? failureReason, DateTime now, CancellationToken cancellationToken = default)
    {
        if (!FinishStatuses.Contains(terminalStatus))
        {
            throw new ArgumentOutOfRangeException(nameof(terminalStatus), terminalStatus, "Only Completed, CompletedWithWarnings, Failed or Interrupted can finish an import file.");
        }

        var storedReason = RetryableStatuses.Contains(terminalStatus) ? ClampReason(failureReason) : null;

        var affected = await OwnedImporting(id, instanceId, attempt)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(f => f.Status, terminalStatus)
                    .SetProperty(f => f.HeartbeatAt, now)
                    .SetProperty(f => f.FinishedAt, now)
                    .SetProperty(f => f.FailureReason, storedReason)
                    .SetProperty(f => f.TotalOrders, progress.TotalOrders)
                    .SetProperty(f => f.ProcessedOrders, progress.ProcessedOrders)
                    .SetProperty(f => f.NewCount, progress.NewCount)
                    .SetProperty(f => f.UpdatedCount, progress.UpdatedCount)
                    .SetProperty(f => f.UnchangedCount, progress.UnchangedCount)
                    .SetProperty(f => f.SupersededCount, progress.SupersededCount)
                    .SetProperty(f => f.RejectedCount, progress.RejectedCount),
                cancellationToken);

        return affected == SingleRow;
    }

    public async Task<int> ReapStaleAsync(DateTime cutoff, CancellationToken cancellationToken = default)
    {
        return await _context.ErpImportFiles
            .Where(f => f.Status == ErpImportFileStatus.Importing && (f.HeartbeatAt ?? f.StartedAt) < cutoff)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(f => f.Status, ErpImportFileStatus.Interrupted)
                    .SetProperty(f => f.FinishedAt, f => f.HeartbeatAt ?? f.StartedAt)
                    .SetProperty(f => f.FailureReason, ErpImportFileFailureReasons.StaleHeartbeat),
                cancellationToken);
    }

    public async Task<bool> TryResetForRetryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var affected = await _context.ErpImportFiles
            .Where(f => f.Id == id && RetryableStatuses.Contains(f.Status))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(f => f.Status, ErpImportFileStatus.Received)
                    .SetProperty(f => f.StartedAt, (DateTime?)null)
                    .SetProperty(f => f.HeartbeatAt, (DateTime?)null)
                    .SetProperty(f => f.FinishedAt, (DateTime?)null)
                    .SetProperty(f => f.ClaimedBy, (string?)null)
                    .SetProperty(f => f.FailureReason, (string?)null)
                    .SetProperty(f => f.TotalOrders, (int?)null)
                    .SetProperty(f => f.ProcessedOrders, 0)
                    .SetProperty(f => f.NewCount, 0)
                    .SetProperty(f => f.UpdatedCount, 0)
                    .SetProperty(f => f.UnchangedCount, 0)
                    .SetProperty(f => f.SupersededCount, 0)
                    .SetProperty(f => f.RejectedCount, 0),
                cancellationToken);

        return affected == SingleRow;
    }

    public async Task<bool> TryMarkFailedAsync(Guid id, string failureReason, DateTime now, CancellationToken cancellationToken = default)
    {
        var storedReason = ClampReason(failureReason);
        var affected = await _context.ErpImportFiles
            .Where(f => f.Id == id && RetryableStatuses.Contains(f.Status))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(f => f.Status, ErpImportFileStatus.Failed)
                    .SetProperty(f => f.FailureReason, storedReason)
                    .SetProperty(f => f.FinishedAt, now),
                cancellationToken);

        return affected == SingleRow;
    }

    public async Task<bool> TrySoftDeleteAsync(Guid id, DateTime now, CancellationToken cancellationToken = default)
    {
        var deletedBy = _context.CurrentAuditActorName();
        var affected = await _context.ErpImportFiles
            .Where(f => f.Id == id && RetryableStatuses.Contains(f.Status))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(f => f.IsDeleted, true)
                    .SetProperty(f => f.DeletedTime, now)
                    .SetProperty(f => f.CurrentUserDeleted, deletedBy),
                cancellationToken);

        return affected == SingleRow;
    }

    public async Task<bool> TryApplyLegacyStateAsync(Guid id, string storageKey, ErpImportFileStatus status, string? failureReason, DateTime finishedAt, CancellationToken cancellationToken = default)
    {
        if (!FinishStatuses.Contains(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "A migrated legacy file can only be Completed, CompletedWithWarnings, Failed or Interrupted.");
        }

        var storedReason = RetryableStatuses.Contains(status) ? ClampReason(failureReason) : null;

        var affected = await _context.ErpImportFiles
            .Where(f => f.Id == id && f.Status != ErpImportFileStatus.Importing)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(f => f.StorageKey, storageKey)
                    .SetProperty(f => f.Status, status)
                    .SetProperty(f => f.FailureReason, storedReason)
                    .SetProperty(f => f.StartedAt, (DateTime?)null)
                    .SetProperty(f => f.HeartbeatAt, (DateTime?)null)
                    .SetProperty(f => f.FinishedAt, finishedAt)
                    .SetProperty(f => f.ClaimedBy, (string?)null)
                    .SetProperty(f => f.TotalOrders, (int?)null)
                    .SetProperty(f => f.ProcessedOrders, 0)
                    .SetProperty(f => f.NewCount, 0)
                    .SetProperty(f => f.UpdatedCount, 0)
                    .SetProperty(f => f.UnchangedCount, 0)
                    .SetProperty(f => f.SupersededCount, 0)
                    .SetProperty(f => f.RejectedCount, 0),
                cancellationToken);

        return affected == SingleRow;
    }

    public async Task<List<ErpImportFileRetentionCandidate>> ListExpiredAsync(Guid dropPointId, DateTime cutoff, int limit, CancellationToken cancellationToken = default)
    {
        return await _context.ErpImportFiles
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(f => f.DropPointId == dropPointId
                && ((!f.IsDeleted
                        && ProcessedStatuses.Contains(f.Status)
                        && (f.FinishedAt < cutoff || (f.FinishedAt == null && f.ReceivedAt < cutoff)))
                    || (f.IsDeleted && f.DeletedTime < cutoff)))
            .OrderBy(f => f.IsDeleted ? f.DeletedTime : (f.FinishedAt ?? f.ReceivedAt))
            .ThenBy(f => f.Id)
            .Take(limit)
            .Select(f => new ErpImportFileRetentionCandidate(f.Id, f.StorageKey, f.IsDeleted))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> TryDeleteExpiredProcessedAsync(Guid id, DateTime cutoff, CancellationToken cancellationToken = default)
    {
        var affected = await _context.ErpImportFiles
            .Where(f => f.Id == id
                && ProcessedStatuses.Contains(f.Status)
                && (f.FinishedAt < cutoff || (f.FinishedAt == null && f.ReceivedAt < cutoff)))
            .ExecuteDeleteAsync(cancellationToken);

        return affected == SingleRow;
    }

    public async Task<bool> TryPurgeExpiredDeletedAsync(Guid id, DateTime cutoff, CancellationToken cancellationToken = default)
    {
        var affected = await _context.ErpImportFiles
            .IgnoreQueryFilters()
            .Where(f => f.Id == id && f.IsDeleted && f.DeletedTime < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

        return affected == SingleRow;
    }

    private IQueryable<ErpImportFile> OwnedImporting(Guid id, string instanceId, int attempt)
    {
        return _context.ErpImportFiles
            .Where(f => f.Id == id && f.Status == ErpImportFileStatus.Importing && f.ClaimedBy == instanceId && f.Attempt == attempt);
    }

    private static string? ClampReason(string? failureReason)
    {
        if (failureReason is null || failureReason.Length <= ErpImportFileLimits.FailureReasonMaxLength)
        {
            return failureReason;
        }

        return failureReason[..ErpImportFileLimits.FailureReasonMaxLength];
    }
}
