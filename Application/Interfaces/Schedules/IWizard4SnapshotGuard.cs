// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

/// <summary>
/// Keeps a background optimisation pass honest about the plan it started from. The pass reads the
/// schedule twice - once as a bitmap, once as an objective context - and a change between those two
/// reads produces a snapshot that never existed. The guard runs both reads inside one repeatable-read
/// transaction and hands out a fingerprint so the pass can tell afterwards whether the plan moved.
/// </summary>
public interface IWizard4SnapshotGuard
{
    /// <summary>
    /// Runs the read phase inside one repeatable-read transaction, so every read sees the same state.
    /// </summary>
    /// <typeparam name="T">Type the read phase produces.</typeparam>
    /// <param name="readPhase">The reads to perform; must not write.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<T> ExecuteInSnapshotAsync<T>(Func<Task<T>> readPhase, CancellationToken ct);

    /// <summary>
    /// Computes the fingerprint of the real plan for one selection.
    /// </summary>
    /// <param name="agentIds">Agents of the selection.</param>
    /// <param name="from">First day of the period, inclusive.</param>
    /// <param name="until">Last day of the period, inclusive.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Wizard4PlanFingerprint> ComputeFingerprintAsync(
        IReadOnlyList<Guid> agentIds, DateOnly from, DateOnly until, CancellationToken ct);
}
