// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Manages SignalR connections with their active date ranges, optional group selection,
/// and current AnalyseToken (null = Original view, otherwise a scenario token).
/// Enables targeted sending of notifications to connections based on date, group and scenario.
/// </summary>
/// <param name="_connectionRanges">Thread-safe map: ConnectionId -> (DateRange, SelectedGroupId, AnalyseToken)</param>

using System.Collections.Concurrent;

namespace Klacks.Api.Infrastructure.Hubs;

public interface IConnectionDateRangeTracker
{
    void RegisterConnection(string connectionId, DateOnly startDate, DateOnly endDate, Guid? analyseToken);
    void UnregisterConnection(string connectionId);
    void SetSelectedGroup(string connectionId, Guid? selectedGroupId);
    void SetAnalyseToken(string connectionId, Guid? analyseToken);
    IEnumerable<string> GetConnectionsForDate(DateOnly date, Guid? analyseToken, string? excludeConnectionId = null);
    IEnumerable<string> GetConnectionsForDateRange(DateOnly startDate, DateOnly endDate, Guid? analyseToken, string? excludeConnectionId = null);
    (List<string> AllGroupConnections, Dictionary<Guid, List<string>> GroupConnections) GetConnectionsGroupedBySelectedGroup(Guid? analyseToken);
    (DateOnly Start, DateOnly End)? GetRegisteredDateRange(string connectionId);
    Guid? GetSelectedGroup(string connectionId);
    Guid? GetAnalyseToken(string connectionId);
}

public class ConnectionDateRangeTracker : IConnectionDateRangeTracker
{
    private readonly ConcurrentDictionary<string, (DateOnly StartDate, DateOnly EndDate, Guid? SelectedGroupId, Guid? AnalyseToken)> _connectionRanges = new();

    public (DateOnly Start, DateOnly End)? GetRegisteredDateRange(string connectionId)
    {
        if (_connectionRanges.TryGetValue(connectionId, out var range))
        {
            return (range.StartDate, range.EndDate);
        }
        return null;
    }

    public Guid? GetSelectedGroup(string connectionId)
    {
        if (_connectionRanges.TryGetValue(connectionId, out var range))
        {
            return range.SelectedGroupId;
        }
        return null;
    }

    public Guid? GetAnalyseToken(string connectionId)
    {
        if (_connectionRanges.TryGetValue(connectionId, out var range))
        {
            return range.AnalyseToken;
        }
        return null;
    }

    public void RegisterConnection(string connectionId, DateOnly startDate, DateOnly endDate, Guid? analyseToken)
    {
        var selectedGroup = _connectionRanges.TryGetValue(connectionId, out var existing)
            ? existing.SelectedGroupId
            : (Guid?)null;
        _connectionRanges[connectionId] = (startDate, endDate, selectedGroup, analyseToken);
    }

    public void UnregisterConnection(string connectionId)
    {
        _connectionRanges.TryRemove(connectionId, out _);
    }

    public void SetSelectedGroup(string connectionId, Guid? selectedGroupId)
    {
        if (_connectionRanges.TryGetValue(connectionId, out var current))
        {
            _connectionRanges[connectionId] = (current.StartDate, current.EndDate, selectedGroupId, current.AnalyseToken);
        }
    }

    public void SetAnalyseToken(string connectionId, Guid? analyseToken)
    {
        if (_connectionRanges.TryGetValue(connectionId, out var current))
        {
            _connectionRanges[connectionId] = (current.StartDate, current.EndDate, current.SelectedGroupId, analyseToken);
        }
    }

    public IEnumerable<string> GetConnectionsForDate(DateOnly date, Guid? analyseToken, string? excludeConnectionId = null)
    {
        foreach (var (connectionId, range) in _connectionRanges)
        {
            if (excludeConnectionId != null && connectionId == excludeConnectionId)
                continue;

            if (range.AnalyseToken != analyseToken)
                continue;

            if (date >= range.StartDate && date <= range.EndDate)
            {
                yield return connectionId;
            }
        }
    }

    public IEnumerable<string> GetConnectionsForDateRange(DateOnly startDate, DateOnly endDate, Guid? analyseToken, string? excludeConnectionId = null)
    {
        foreach (var (connectionId, range) in _connectionRanges)
        {
            if (excludeConnectionId != null && connectionId == excludeConnectionId)
                continue;

            if (range.AnalyseToken != analyseToken)
                continue;

            if (RangesOverlap(range.StartDate, range.EndDate, startDate, endDate))
            {
                yield return connectionId;
            }
        }
    }

    public (List<string> AllGroupConnections, Dictionary<Guid, List<string>> GroupConnections) GetConnectionsGroupedBySelectedGroup(Guid? analyseToken)
    {
        var allGroupConnections = new List<string>();
        var groupConnections = new Dictionary<Guid, List<string>>();

        foreach (var (connectionId, range) in _connectionRanges)
        {
            if (range.AnalyseToken != analyseToken)
                continue;

            if (range.SelectedGroupId == null)
            {
                allGroupConnections.Add(connectionId);
            }
            else
            {
                if (!groupConnections.TryGetValue(range.SelectedGroupId.Value, out var list))
                {
                    list = [];
                    groupConnections[range.SelectedGroupId.Value] = list;
                }
                list.Add(connectionId);
            }
        }

        return (allGroupConnections, groupConnections);
    }

    private static bool RangesOverlap(DateOnly aStart, DateOnly aEnd, DateOnly bStart, DateOnly bEnd)
    {
        return aStart <= bEnd && bStart <= aEnd;
    }
}
