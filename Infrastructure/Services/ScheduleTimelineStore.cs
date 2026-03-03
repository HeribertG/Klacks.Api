// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Collections.Concurrent;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Klacks.Api.Infrastructure.Services;

public class ScheduleTimelineStore : IScheduleTimelineStore
{
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromHours(24);

    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, byte> _keys = new();

    public ScheduleTimelineStore(IMemoryCache cache)
    {
        _cache = cache;
    }

    public void SetTimeline(Guid clientId, DateOnly date, ClientDayTimeline timeline)
    {
        var key = BuildKey(clientId, date);
        var options = new MemoryCacheEntryOptions
        {
            SlidingExpiration = CacheExpiration
        };
        options.RegisterPostEvictionCallback((evictedKey, _, _, _) =>
        {
            _keys.TryRemove(evictedKey.ToString()!, out _);
        });
        _cache.Set(key, timeline, options);
        _keys[key] = 0;
    }

    public void RemoveTimeline(Guid clientId, DateOnly date)
    {
        var key = BuildKey(clientId, date);
        _cache.Remove(key);
        _keys.TryRemove(key, out _);
    }

    public ClientDayTimeline? GetTimeline(Guid clientId, DateOnly date)
    {
        var key = BuildKey(clientId, date);
        return _cache.TryGetValue(key, out ClientDayTimeline? timeline) ? timeline : null;
    }

    public List<ClientDayTimeline> GetTimelinesForDateRange(DateOnly startDate, DateOnly endDate)
    {
        var result = new List<ClientDayTimeline>();
        foreach (var key in _keys.Keys)
        {
            if (_cache.TryGetValue(key, out ClientDayTimeline? timeline) &&
                timeline != null &&
                timeline.Date >= startDate &&
                timeline.Date <= endDate)
            {
                result.Add(timeline);
            }
        }
        return result;
    }

    private static string BuildKey(Guid clientId, DateOnly date) => $"{clientId}_{date:yyyy-MM-dd}";
}
