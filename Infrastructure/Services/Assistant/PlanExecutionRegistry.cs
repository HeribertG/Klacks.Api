// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Thread-safe, process-local implementation of IPlanExecutionRegistry backed by a
/// ConcurrentDictionary of CancellationTokenSource. Registered as a singleton so the controller's
/// fire-and-forget execution task and the abort endpoint share the same instance.
/// </summary>

using System.Collections.Concurrent;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class PlanExecutionRegistry : IPlanExecutionRegistry
{
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _running = new();

    public CancellationToken Register(Guid planId)
    {
        var cts = new CancellationTokenSource();
        var previous = _running.AddOrUpdate(planId, cts, (_, existing) =>
        {
            existing.Dispose();
            return cts;
        });
        return previous.Token;
    }

    public void Unregister(Guid planId)
    {
        if (_running.TryRemove(planId, out var cts))
        {
            cts.Dispose();
        }
    }

    public bool TryRequestCancellation(Guid planId)
    {
        if (!_running.TryGetValue(planId, out var cts))
        {
            return false;
        }

        try
        {
            cts.Cancel();
            return true;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }
}
