// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Imports;

namespace Klacks.Api.Domain.Interfaces.Imports;

/// <summary>
/// The erp_import_file ledger: one row per delivered ERP order file, the single source of truth for its
/// state and history.
/// </summary>
/// <remarks>
/// Mixed commit convention, by design:
/// <list type="bullet">
/// <item><see cref="Add"/> is stage-only; the caller commits through IUnitOfWork.CompleteAsync().</item>
/// <item>The state transitions (<see cref="TryClaimAsync"/>, <see cref="HeartbeatAsync"/>,
/// <see cref="FinishAsync"/>, <see cref="ReapStaleAsync"/>, <see cref="TryResetForRetryAsync"/>,
/// <see cref="TryMarkFailedAsync"/>, <see cref="TrySoftDeleteAsync"/>, <see cref="TryApplyLegacyStateAsync"/>)
/// and the retention deletes (<see cref="TryDeleteExpiredProcessedAsync"/>, <see cref="TryPurgeExpiredDeletedAsync"/>)
/// are self-committing: each is a single conditional UPDATE or DELETE issued through ExecuteUpdateAsync or
/// ExecuteDeleteAsync. They bypass
/// the change tracker, so they neither flush nor see staged changes of other repositories, and a tracked
/// ErpImportFile instance is NOT refreshed by them.</item>
/// </list>
/// The transitions DO enlist in an ambient transaction open on the shared context: called inside
/// IUnitOfWork.ExecuteInTransactionAsync they are rolled back together with it. Call them outside the
/// per-order transactions. Reads are AsNoTracking so no ledger row is ever flushed by another
/// repository's SaveChangesAsync. The conditional transitions return true only when exactly one row
/// matched, which makes them safe across several API instances.
/// </remarks>
public interface IErpImportFileRepository
{
    /// <summary>Stages a new ledger row (stage-only; the caller commits).</summary>
    void Add(ErpImportFile file);

    /// <summary>
    /// Stops tracking a staged row, e.g. after its commit hit the unique storage-key index because another
    /// caller registered the same file concurrently; otherwise the next commit would try to insert it again.
    /// </summary>
    void Detach(ErpImportFile file);

    Task<List<ErpImportFile>> GetByStorageKeysAsync(IReadOnlyCollection<string> storageKeys, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns storage key and deleted flag of every row -- soft-deleted ones included -- whose key is one of
    /// storageKeys (projection only, no full rows). A key can appear twice: once deleted, once live.
    /// </summary>
    Task<List<ErpImportFileKeyState>> GetKeyStatesAsync(IReadOnlyCollection<string> storageKeys, CancellationToken cancellationToken = default);

    /// <summary>Returns the row with the given id, untracked, or null when it does not exist or was deleted.</summary>
    Task<ErpImportFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns the rows with the given ids, untracked; deleted rows are left out.</summary>
    Task<List<ErpImportFile>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts the drop point's rows per listing group (uncapped). Importing rows whose last heartbeat (or start)
    /// is older than staleCutoff count as failed, exactly as the listing reports them.
    /// </summary>
    Task<ErpImportFileStatusCounts> CountByStatusAsync(Guid dropPointId, DateTime staleCutoff, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the drop point's most recently finished row (Completed, CompletedWithWarnings, Failed or
    /// Interrupted, by finish time), untracked, or null when no file has finished yet.
    /// </summary>
    Task<ErpImportFile?> GetLatestFinishedAsync(Guid dropPointId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the earliest row of the drop point with the same SHA-256, ignoring the row with id
    /// excludeId, or null when there is none. Only feeds the duplicate hint; it never blocks an import.
    /// </summary>
    Task<ErpImportFile?> FindDuplicateAsync(Guid dropPointId, string sha256, Guid? excludeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the drop point's rows, newest received first: every Received, Importing, Failed and
    /// Interrupted row plus the newest processedCap Completed/CompletedWithWarnings rows.
    /// </summary>
    Task<List<ErpImportFile>> ListForDropPointAsync(Guid dropPointId, int processedCap, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the drop point's Received rows, oldest received first (then by id), untracked. These are the
    /// files the runner still has to claim and import.
    /// </summary>
    Task<List<ErpImportFile>> ListReceivedAsync(Guid dropPointId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Self-committing. Received -> Importing for the given instance, compare-and-swap on the attempt number
    /// the caller read: matches only while the row is Received AND still carries expectedAttempt, then sets
    /// the attempt to expectedAttempt + 1, the started/heartbeat time and claimed_by, and resets the
    /// counters. The caller therefore knows its attempt number (expectedAttempt + 1) without a read-back,
    /// and a row that was claimed, reset and re-listed in between is not claimed with a stale number.
    /// False when the row is not Received or its attempt moved on.
    /// </summary>
    /// <remarks>
    /// A claim whose commit succeeded but whose result was lost (e.g. a connection drop retried by the
    /// execution strategy) reports false and the caller skips the file; the row then stays Importing
    /// without heartbeats and the reaper flips it to Interrupted after ErpImportFileTiming.StaleAfter,
    /// from where it can be retried. Accepted: rare, and never imports a file twice.
    /// </remarks>
    Task<bool> TryClaimAsync(Guid id, int expectedAttempt, string instanceId, DateTime now, CancellationToken cancellationToken = default);

    /// <summary>
    /// Self-committing. Writes the progress and the heartbeat time while the row is Importing, still
    /// claimed by instanceId AND still in the given attempt -- a stale run of the same process can never
    /// write into a later attempt. False means the row was reaped, reset or claimed again: stop the file.
    /// </summary>
    Task<bool> HeartbeatAsync(Guid id, string instanceId, int attempt, ErpImportFileProgress progress, DateTime now, CancellationToken cancellationToken = default);

    /// <summary>
    /// Self-committing. Importing -> terminalStatus (Completed, CompletedWithWarnings, Failed or
    /// Interrupted) under the same guard as <see cref="HeartbeatAsync"/>. Interrupted is used when the
    /// importing process itself hit an unexpected error. The failure reason is stored only for Failed and
    /// Interrupted and is clamped to the column length.
    /// </summary>
    Task<bool> FinishAsync(Guid id, string instanceId, int attempt, ErpImportFileStatus terminalStatus, ErpImportFileProgress progress, string? failureReason, DateTime now, CancellationToken cancellationToken = default);

    /// <summary>
    /// Self-committing. Flips every Importing row (all drop points) whose last heartbeat is older than
    /// cutoff to Interrupted, with the last heartbeat as finish time. Returns the number of rows flipped.
    /// </summary>
    Task<int> ReapStaleAsync(DateTime cutoff, CancellationToken cancellationToken = default);

    /// <summary>
    /// Self-committing. Failed or Interrupted -> Received: clears the reason, the counters, the claim and
    /// the start/heartbeat/finish times; the attempt number is kept. False for any other status.
    /// </summary>
    Task<bool> TryResetForRetryAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Self-committing. Failed or Interrupted -> Failed with the given reason (clamped to the column length)
    /// and finish time, e.g. when a retry finds the stored file changed or missing. False for any other status.
    /// </summary>
    Task<bool> TryMarkFailedAsync(Guid id, string failureReason, DateTime now, CancellationToken cancellationToken = default);

    /// <summary>
    /// Self-committing. Soft-deletes a Failed or Interrupted row (IsDeleted, DeletedTime, CurrentUserDeleted
    /// are written here because the update bypasses the context's soft-delete handling; the actor is the
    /// context's audit actor, as in every other bulk update). False for any other status or an already deleted row.
    /// </summary>
    Task<bool> TrySoftDeleteAsync(Guid id, DateTime now, CancellationToken cancellationToken = default);

    /// <summary>
    /// Self-committing. Writes the state of a file found in a pre-ledger sub-folder onto an existing row
    /// (one registered by an upload before the old import moved its file): storage key, terminal status,
    /// failure reason (Failed/Interrupted only, clamped) and finish time; the counters are reset and the total
    /// becomes unknown (null). Never touches an Importing row: false then.
    /// </summary>
    Task<bool> TryApplyLegacyStateAsync(Guid id, string storageKey, ErpImportFileStatus status, string? failureReason, DateTime finishedAt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns at most limit rows of the drop point that are due for the retention purge, oldest first, as
    /// id/key/deleted projections: live Completed and CompletedWithWarnings rows that finished before cutoff (a
    /// row without finish time, e.g. a migrated legacy row, counts from its receipt), and soft-deleted rows of any
    /// status whose deletion time is before cutoff (a soft-deleted row without deletion time is never due).
    /// Failed, Interrupted, Received and Importing rows that are not deleted are never returned.
    /// </summary>
    Task<List<ErpImportFileRetentionCandidate>> ListExpiredAsync(Guid dropPointId, DateTime cutoff, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Self-committing. Hard-deletes the row only while it is still a live Completed or CompletedWithWarnings row
    /// that finished (or, without finish time, was received) before cutoff; false when it changed in between.
    /// The database sets ImportFileId of linked exceptions and DuplicateOfFileId of later rows to null.
    /// </summary>
    Task<bool> TryDeleteExpiredProcessedAsync(Guid id, DateTime cutoff, CancellationToken cancellationToken = default);

    /// <summary>
    /// Self-committing. Hard-deletes the row only while it is still soft-deleted with a deletion time before
    /// cutoff; false otherwise. The caller must have confirmed that the row's storage object is gone: the inbox
    /// registration relies on the soft-deleted row to never register a leftover object again.
    /// </summary>
    Task<bool> TryPurgeExpiredDeletedAsync(Guid id, DateTime cutoff, CancellationToken cancellationToken = default);
}
