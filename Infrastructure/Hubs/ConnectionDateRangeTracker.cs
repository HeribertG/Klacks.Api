// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Verwaltet SignalR-Connections mit ihren aktiven Datumsbereichen und optionaler Gruppenauswahl.
/// Ermoeglicht gezieltes Senden von Notifications an Connections basierend auf Datum und Gruppe.
/// </summary>
/// <param name="_connectionRanges">Thread-sichere Map: ConnectionId → (DateRange, SelectedGroupId)</param>

using System.Collections.Concurrent;

namespace Klacks.Api.Infrastructure.Hubs;

public interface IConnectionDateRangeTracker
{
    void RegisterConnection(string connectionId, DateOnly startDate, DateOnly endDate);
    void UnregisterConnection(string connectionId);
    void SetSelectedGroup(string connectionId, Guid? selectedGroupId);
    IEnumerable<string> GetConnectionsForDate(DateOnly date, string? excludeConnectionId = null);
    IEnumerable<string> GetConnectionsForDateRange(DateOnly startDate, DateOnly endDate, string? excludeConnectionId = null);
    (List<string> AllGroupConnections, Dictionary<Guid, List<string>> GroupConnections) GetConnectionsGroupedBySelectedGroup();
}

public class ConnectionDateRangeTracker : IConnectionDateRangeTracker
{
    private readonly ConcurrentDictionary<string, (DateOnly StartDate, DateOnly EndDate, Guid? SelectedGroupId)> _connectionRanges = new();

    public void RegisterConnection(string connectionId, DateOnly startDate, DateOnly endDate)
    {
        _connectionRanges[connectionId] = (startDate, endDate, null);
    }

    public void UnregisterConnection(string connectionId)
    {
        _connectionRanges.TryRemove(connectionId, out _);
    }

    public void SetSelectedGroup(string connectionId, Guid? selectedGroupId)
    {
        if (_connectionRanges.TryGetValue(connectionId, out var current))
        {
            _connectionRanges[connectionId] = (current.StartDate, current.EndDate, selectedGroupId);
        }
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

    public (List<string> AllGroupConnections, Dictionary<Guid, List<string>> GroupConnections) GetConnectionsGroupedBySelectedGroup()
    {
        var allGroupConnections = new List<string>();
        var groupConnections = new Dictionary<Guid, List<string>>();

        foreach (var (connectionId, range) in _connectionRanges)
        {
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
