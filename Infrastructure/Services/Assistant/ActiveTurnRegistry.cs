// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Thread-safe, process-local implementation of IActiveTurnRegistry backed by a ConcurrentDictionary.
/// Registered as a singleton so the streaming request and the cancel request share one instance, which is
/// why it carries no dependency at all: a scoped or kernel service here would either be captured for the
/// lifetime of the process or close a cycle in the ILLMService graph.
/// </summary>

using System.Collections.Concurrent;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class ActiveTurnRegistry : IActiveTurnRegistry
{
    private readonly ConcurrentDictionary<Guid, ActiveTurn> _turns = new();

    internal int ActiveCount => _turns.Count;

    // The cancellation sources are deliberately never disposed: a source without timers or linked sources
    // holds no unmanaged resource, and disposing one would turn a stop that races Complete into an
    // ObjectDisposedException - the very hazard PlanExecutionRegistry has to catch.
    public CancellationToken Register(Guid turnId, string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        if (turnId == Guid.Empty)
        {
            throw new ArgumentException("A turn id must not be empty.", nameof(turnId));
        }

        var turn = new ActiveTurn(userId, new CancellationTokenSource());
        if (!_turns.TryAdd(turnId, turn))
        {
            throw new InvalidOperationException($"Turn {turnId} is already registered.");
        }

        return turn.Stop.Token;
    }

    public StopRequestOutcome RequestStop(Guid turnId, string userId)
    {
        if (string.IsNullOrWhiteSpace(userId)
            || !_turns.TryGetValue(turnId, out var turn)
            || !string.Equals(turn.UserId, userId, StringComparison.Ordinal))
        {
            return StopRequestOutcome.NotFound;
        }

        turn.Stop.Cancel();
        return StopRequestOutcome.Accepted;
    }

    public bool IsStopRequested(Guid turnId)
    {
        return _turns.TryGetValue(turnId, out var turn) && turn.Stop.IsCancellationRequested;
    }

    public void Complete(Guid turnId)
    {
        _turns.TryRemove(turnId, out _);
    }

    private sealed record ActiveTurn(string UserId, CancellationTokenSource Stop);
}
