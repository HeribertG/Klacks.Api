// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Collections.Concurrent;

namespace Klacks.Api.Infrastructure.Hubs;

public interface IAssistantConnectionTracker
{
    void RegisterConnection(string userId, string connectionId);
    void UnregisterConnection(string connectionId);
    bool IsUserConnected(string userId);
    IEnumerable<string> GetConnectedUserIds();
    IEnumerable<string> GetConnectionIds(string userId);
}

public class AssistantConnectionTracker : IAssistantConnectionTracker
{
    private readonly ConcurrentDictionary<string, ConcurrentBag<string>> _userConnections = new();
    private readonly ConcurrentDictionary<string, string> _connectionToUser = new();

    public void RegisterConnection(string userId, string connectionId)
    {
        _connectionToUser[connectionId] = userId;
        _userConnections.AddOrUpdate(
            userId,
            _ => new ConcurrentBag<string> { connectionId },
            (_, bag) => { bag.Add(connectionId); return bag; });
    }

    public void UnregisterConnection(string connectionId)
    {
        if (!_connectionToUser.TryRemove(connectionId, out var userId))
            return;

        if (!_userConnections.TryGetValue(userId, out var bag))
            return;

        var remaining = new ConcurrentBag<string>(bag.Where(id => id != connectionId));
        if (remaining.IsEmpty)
            _userConnections.TryRemove(userId, out _);
        else
            _userConnections[userId] = remaining;
    }

    public bool IsUserConnected(string userId)
    {
        return _userConnections.TryGetValue(userId, out var bag) && !bag.IsEmpty;
    }

    public IEnumerable<string> GetConnectedUserIds()
    {
        return _userConnections
            .Where(kv => !kv.Value.IsEmpty)
            .Select(kv => kv.Key);
    }

    public IEnumerable<string> GetConnectionIds(string userId)
    {
        return _userConnections.TryGetValue(userId, out var bag)
            ? bag.ToArray()
            : Array.Empty<string>();
    }
}
