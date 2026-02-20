using System.Collections.Concurrent;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Interfaces;

namespace Klacks.Api.Infrastructure.Services;

public class ScheduleTimelineStore : IScheduleTimelineStore
{
    private readonly ConcurrentDictionary<string, ClientDayTimeline> _timelines = new();

    public void SetTimeline(Guid clientId, DateOnly date, ClientDayTimeline timeline)
    {
        var key = BuildKey(clientId, date);
        _timelines[key] = timeline;
    }

    public void RemoveTimeline(Guid clientId, DateOnly date)
    {
        var key = BuildKey(clientId, date);
        _timelines.TryRemove(key, out _);
    }

    public ClientDayTimeline? GetTimeline(Guid clientId, DateOnly date)
    {
        var key = BuildKey(clientId, date);
        return _timelines.GetValueOrDefault(key);
    }

    public List<ClientDayTimeline> GetTimelinesForDateRange(DateOnly startDate, DateOnly endDate)
    {
        return _timelines.Values
            .Where(t => t.Date >= startDate && t.Date <= endDate)
            .ToList();
    }

    private static string BuildKey(Guid clientId, DateOnly date) => $"{clientId}_{date:yyyy-MM-dd}";
}
