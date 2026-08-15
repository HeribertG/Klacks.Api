// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// In-memory tracker mapping assistant hub users to their active SignalR connection ids.
/// State is per process; every member completes synchronously behind the asynchronous contract.
/// </summary>
/// <param name="_userConnections">Thread-safe map: UserId -> active ConnectionIds</param>
/// <param name="_connectionToUser">Thread-safe reverse map: ConnectionId -> UserId</param>

using System.Collections.Concurrent;

namespace Klacks.Api.Infrastructure.Hubs;

public class AssistantConnectionTracker : IAssistantConnectionTracker
{
    private readonly ConcurrentDictionary<string, ConcurrentBag<string>> _userConnections = new();
    private readonly ConcurrentDictionary<string, string> _connectionToUser = new();

    public Task RegisterConnectionAsync(string userId, string connectionId, CancellationToken cancellationToken = default)
    {
        _connectionToUser[connectionId] = userId;
        _userConnections.AddOrUpdate(
            userId,
            _ => new ConcurrentBag<string> { connectionId },
            (_, bag) => { bag.Add(connectionId); return bag; });

        return Task.CompletedTask;
    }

    public Task UnregisterConnectionAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        if (!_connectionToUser.TryRemove(connectionId, out var userId))
            return Task.CompletedTask;

        if (!_userConnections.TryGetValue(userId, out var bag))
            return Task.CompletedTask;

        var remaining = new ConcurrentBag<string>(bag.Where(id => id != connectionId));
        if (remaining.IsEmpty)
            _userConnections.TryRemove(userId, out _);
        else
            _userConnections[userId] = remaining;

        return Task.CompletedTask;
    }

    public Task<bool> IsUserConnectedAsync(string userId)
    {
        return Task.FromResult(_userConnections.TryGetValue(userId, out var bag) && !bag.IsEmpty);
    }

    public Task<IReadOnlyList<string>> GetConnectedUserIdsAsync()
    {
        IReadOnlyList<string> userIds = _userConnections
            .Where(kv => !kv.Value.IsEmpty)
            .Select(kv => kv.Key)
            .ToList();

        return Task.FromResult(userIds);
    }

    public Task<IReadOnlyList<string>> GetConnectionIdsAsync(string userId)
    {
        IReadOnlyList<string> connectionIds = _userConnections.TryGetValue(userId, out var bag)
            ? bag.ToArray()
            : Array.Empty<string>();

        return Task.FromResult(connectionIds);
    }
}
