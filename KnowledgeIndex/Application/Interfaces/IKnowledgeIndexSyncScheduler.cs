// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.KnowledgeIndex.Domain;

namespace Klacks.Api.KnowledgeIndex.Application.Interfaces;

/// <summary>
/// Runs the knowledge index synchronization outside the caller's request: at most one run at a time,
/// and any number of requests that arrive while a run executes collapse into a single follow-up run.
/// Failures never reach the caller; they are logged and reported through <see cref="Status"/>.
/// </summary>
public interface IKnowledgeIndexSyncScheduler
{
    /// <summary>Current state of the background synchronization; safe to read from any thread.</summary>
    KnowledgeIndexSyncStatus Status { get; }

    /// <summary>
    /// Marks the index as out of date and returns immediately. A run starts if none is active;
    /// otherwise one more run follows the active one, however many requests arrive meanwhile.
    /// </summary>
    /// <param name="reason">Short description of the change, used in log lines and the status.</param>
    void Request(string reason);

    /// <summary>
    /// Requests a run and waits until a run that STARTED after this call has finished, so the index
    /// reflects every change persisted before the call. A run already in flight does not count: it
    /// may have read the catalogue before the caller's change. Completes without throwing when that
    /// run fails (see <see cref="Status"/>) or when the application is stopping.
    /// </summary>
    /// <param name="reason">Short description of the change, used in log lines and the status.</param>
    /// <param name="cancellationToken">Stops the caller from waiting; the run itself continues.</param>
    Task RunNowAsync(string reason, CancellationToken cancellationToken);
}
