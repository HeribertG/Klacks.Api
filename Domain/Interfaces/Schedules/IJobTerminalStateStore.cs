// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

/// <summary>
/// Database access behind JobTerminalStateCache. Scoped (one DataBaseContext per instance): a singleton
/// consumer must resolve it from its own scope per operation, never through its constructor.
/// Self-committing: EvictExpiredAsync and TryAddAsync call SaveChangesAsync themselves.
/// </summary>
public interface IJobTerminalStateStore
{
    /// <summary>
    /// Reads the row of a job without tracking and without looking at its expiry.
    /// </summary>
    /// <param name="jobId">The job whose terminal state is wanted</param>
    /// <param name="cancellationToken">Cancels the read</param>
    Task<JobTerminalStateRow?> FindAsync(Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes every row that expired at or before <paramref name="utcNow"/>. A concurrent writer that removed
    /// the same rows first is not an error.
    /// </summary>
    /// <param name="utcNow">The moment against which ExpiresAtUtc is compared</param>
    /// <param name="cancellationToken">Cancels the eviction</param>
    Task EvictExpiredAsync(DateTime utcNow, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts the row and commits. Returns false, without throwing, when the job already carries a terminal
    /// state (unique index on JobId); any other write failure propagates.
    /// </summary>
    /// <param name="row">The terminal state to store</param>
    /// <param name="cancellationToken">Cancels the insert</param>
    Task<bool> TryAddAsync(JobTerminalStateRow row, CancellationToken cancellationToken = default);
}
