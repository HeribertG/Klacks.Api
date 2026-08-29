// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Thread-safe in-memory tracker of each user's last chat interaction time. Used by the proactive
/// trigger service to skip users who are mid-conversation.
/// </summary>
/// <param name="timeProvider">Clock activity timestamps are read from, injected so a test can drive it.</param>

using System.Collections.Concurrent;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class UserActivityTracker : IUserActivityTracker
{
    private readonly ConcurrentDictionary<string, DateTime> _lastActiveUtc = new();
    private readonly TimeProvider _timeProvider;

    public UserActivityTracker(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public void MarkActive(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        _lastActiveUtc[userId] = _timeProvider.GetUtcNow().UtcDateTime;
    }

    public bool IsRecentlyActive(string userId, TimeSpan window)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return false;
        }

        return _lastActiveUtc.TryGetValue(userId, out var lastActive)
               && _timeProvider.GetUtcNow().UtcDateTime - lastActive < window;
    }
}
