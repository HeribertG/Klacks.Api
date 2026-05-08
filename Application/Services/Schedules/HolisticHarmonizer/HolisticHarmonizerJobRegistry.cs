// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Collections.Concurrent;

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;

/// <summary>
/// In-memory registry of running Holistic Harmonizer jobs and their linked cancellation sources.
/// Singleton; survives HTTP-request scope so background <c>Task.Run</c> can still be cancelled.
/// </summary>
public sealed class HolisticHarmonizerJobRegistry
{
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _jobs = new();

    public CancellationTokenSource Register(Guid jobId, CancellationToken externalCt)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(externalCt);
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
