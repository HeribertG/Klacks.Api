// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Manages SignalR connections with their active date ranges, optional group selection,
/// and current AnalyseToken (null = Original view, otherwise a scenario token).
/// Enables targeted sending of notifications to connections based on date, group and scenario.
/// State is per process; every member completes synchronously behind the asynchronous contract.
/// </summary>
/// <param name="_connectionRanges">Thread-safe map: ConnectionId -> (DateRange, SelectedGroupId, AnalyseToken)</param>
/// <param name="_pendingSelectedGroups">Group selections arriving before the connection registered a date range</param>

using System.Collections.Concurrent;

namespace Klacks.Api.Infrastructure.Hubs;

public class ConnectionDateRangeTracker : IConnectionDateRangeTracker
{
    private readonly ConcurrentDictionary<string, (DateOnly StartDate, DateOnly EndDate, Guid? SelectedGroupId, Guid? AnalyseToken)> _connectionRanges = new();
    private readonly ConcurrentDictionary<string, Guid?> _pendingSelectedGroups = new();

    public Task<(DateOnly Start, DateOnly End)?> GetRegisteredDateRangeAsync(string connectionId)
    {
        if (_connectionRanges.TryGetValue(connectionId, out var range))
        {
            return Task.FromResult<(DateOnly Start, DateOnly End)?>((range.StartDate, range.EndDate));
        }
        return Task.FromResult<(DateOnly Start, DateOnly End)?>(null);
    }

    public Task<Guid?> GetSelectedGroupAsync(string connectionId)
    {
        if (_connectionRanges.TryGetValue(connectionId, out var range))
        {
            return Task.FromResult(range.SelectedGroupId);
        }
        return Task.FromResult<Guid?>(null);
    }

    public Task<Guid?> GetAnalyseTokenAsync(string connectionId)
    {
        if (_connectionRanges.TryGetValue(connectionId, out var range))
        {
            return Task.FromResult(range.AnalyseToken);
        }
        return Task.FromResult<Guid?>(null);
    }

    public Task RegisterConnectionAsync(string connectionId, DateOnly startDate, DateOnly endDate, Guid? analyseToken, CancellationToken cancellationToken = default)
    {
        Guid? selectedGroup;
        if (_connectionRanges.TryGetValue(connectionId, out var existing))
        {
            selectedGroup = existing.SelectedGroupId;
        }
        else if (_pendingSelectedGroups.TryRemove(connectionId, out var pending))
        {
            selectedGroup = pending;
        }
        else
        {
            selectedGroup = null;
        }
        _connectionRanges[connectionId] = (startDate, endDate, selectedGroup, analyseToken);

        return Task.CompletedTask;
    }

    public Task UnregisterConnectionAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        _connectionRanges.TryRemove(connectionId, out _);
        _pendingSelectedGroups.TryRemove(connectionId, out _);

        return Task.CompletedTask;
    }

    public Task SetSelectedGroupAsync(string connectionId, Guid? selectedGroupId, CancellationToken cancellationToken = default)
    {
        if (_connectionRanges.TryGetValue(connectionId, out var current))
        {
            _connectionRanges[connectionId] = (current.StartDate, current.EndDate, selectedGroupId, current.AnalyseToken);
        }
        else
        {
            _pendingSelectedGroups[connectionId] = selectedGroupId;
        }

        return Task.CompletedTask;
    }

    public Task SetAnalyseTokenAsync(string connectionId, Guid? analyseToken, CancellationToken cancellationToken = default)
    {
        if (_connectionRanges.TryGetValue(connectionId, out var current))
        {
            _connectionRanges[connectionId] = (current.StartDate, current.EndDate, current.SelectedGroupId, analyseToken);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> GetConnectionsForDateAsync(DateOnly date, Guid? analyseToken, string? excludeConnectionId = null)
    {
        var matches = new List<string>();

        foreach (var (connectionId, range) in _connectionRanges)
        {
            if (excludeConnectionId != null && connectionId == excludeConnectionId)
                continue;

            if (range.AnalyseToken != analyseToken)
                continue;

            if (date >= range.StartDate && date <= range.EndDate)
            {
                matches.Add(connectionId);
            }
        }

        return Task.FromResult<IReadOnlyList<string>>(matches);
    }

    public Task<IReadOnlyList<string>> GetConnectionsForDateRangeAsync(DateOnly startDate, DateOnly endDate, Guid? analyseToken, string? excludeConnectionId = null)
    {
        var matches = new List<string>();

        foreach (var (connectionId, range) in _connectionRanges)
        {
            if (excludeConnectionId != null && connectionId == excludeConnectionId)
                continue;

            if (range.AnalyseToken != analyseToken)
                continue;

            if (RangesOverlap(range.StartDate, range.EndDate, startDate, endDate))
            {
                matches.Add(connectionId);
            }
        }

        return Task.FromResult<IReadOnlyList<string>>(matches);
    }

    public Task<(List<string> AllGroupConnections, Dictionary<Guid, List<string>> GroupConnections)> GetConnectionsGroupedBySelectedGroupAsync(Guid? analyseToken)
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

        return Task.FromResult((allGroupConnections, groupConnections));
    }

    private static bool RangesOverlap(DateOnly aStart, DateOnly aEnd, DateOnly bStart, DateOnly bEnd)
    {
        return aStart <= bEnd && bStart <= aEnd;
    }
}
