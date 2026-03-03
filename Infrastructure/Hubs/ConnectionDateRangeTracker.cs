// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Collections.Concurrent;

namespace Klacks.Api.Infrastructure.Hubs;

public interface IConnectionDateRangeTracker
{
    void RegisterConnection(string connectionId, DateOnly startDate, DateOnly endDate);
    void UnregisterConnection(string connectionId);
    IEnumerable<string> GetConnectionsForDate(DateOnly date, string? excludeConnectionId = null);
    IEnumerable<string> GetConnectionsForDateRange(DateOnly startDate, DateOnly endDate, string? excludeConnectionId = null);
}

public class ConnectionDateRangeTracker : IConnectionDateRangeTracker
{
    private readonly ConcurrentDictionary<string, (DateOnly StartDate, DateOnly EndDate)> _connectionRanges = new();

    public void RegisterConnection(string connectionId, DateOnly startDate, DateOnly endDate)
    {
        _connectionRanges[connectionId] = (startDate, endDate);
    }

    public void UnregisterConnection(string connectionId)
    {
        _connectionRanges.TryRemove(connectionId, out _);
    }

    public IEnumerable<string> GetConnectionsForDate(DateOnly date, string? excludeConnectionId = null)
    {
        foreach (var (connectionId, range) in _connectionRanges)
        {
            if (excludeConnectionId != null && connectionId == excludeConnectionId)
                continue;

            if (date >= range.StartDate && date <= range.EndDate)
            {
                yield return connectionId;
            }
        }
    }

    public IEnumerable<string> GetConnectionsForDateRange(DateOnly startDate, DateOnly endDate, string? excludeConnectionId = null)
    {
        foreach (var (connectionId, range) in _connectionRanges)
        {
            if (excludeConnectionId != null && connectionId == excludeConnectionId)
                continue;

            if (RangesOverlap(range.StartDate, range.EndDate, startDate, endDate))
            {
                yield return connectionId;
            }
        }
    }

    private static bool RangesOverlap(DateOnly aStart, DateOnly aEnd, DateOnly bStart, DateOnly bEnd)
    {
        return aStart <= bEnd && bStart <= aEnd;
    }
}
