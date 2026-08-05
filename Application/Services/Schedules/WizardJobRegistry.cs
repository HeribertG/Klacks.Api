// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Collections.Concurrent;

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// In-memory registry of running wizard jobs and their linked cancellation sources.
/// Singleton; survives HTTP-request scope so that background Task.Run can still be cancelled.
/// </summary>
public sealed class WizardJobRegistry
{
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _jobs = new();

    /// <summary>
    /// Registers a job and returns its cancellation source. Never pass HttpContext.RequestAborted here:
    /// the job outlives the request that started it, so a client disconnect must not cancel it.
    /// </summary>
    /// <param name="jobId">Id of the job being registered.</param>
    /// <param name="lifetimeCt">Host shutdown token; the only ambient reason to abort the job.</param>
    /// <param name="chainCt">Optional parent-job token so a cancelled chain cancels its stages.</param>
    public CancellationTokenSource Register(Guid jobId, CancellationToken lifetimeCt, CancellationToken chainCt = default)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(lifetimeCt, chainCt);
        _jobs[jobId] = cts;
        return cts;
    }

    public bool TryCancel(Guid jobId)
    {
        if (!_jobs.TryGetValue(jobId, out var cts))
        {
            return false;
        }

        cts.Cancel();
        return true;
    }

    public bool IsRunning(Guid jobId) => _jobs.ContainsKey(jobId);

    public void Remove(Guid jobId)
    {
        if (_jobs.TryRemove(jobId, out var cts))
        {
            cts.Dispose();
        }
    }
}
