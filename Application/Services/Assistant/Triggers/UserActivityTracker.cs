// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Thread-safe in-memory tracker of each user's last chat interaction time. Used by the proactive
/// trigger service to skip users who are mid-conversation.
/// </summary>

using System.Collections.Concurrent;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class UserActivityTracker : IUserActivityTracker
{
    private readonly ConcurrentDictionary<string, DateTime> _lastActiveUtc = new();

    public void MarkActive(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        _lastActiveUtc[userId] = DateTime.UtcNow;
    }

    public bool IsRecentlyActive(string userId, TimeSpan window)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return false;
        }

        return _lastActiveUtc.TryGetValue(userId, out var lastActive)
               && DateTime.UtcNow - lastActive < window;
    }
}
