// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Manages SignalR connections with their active date ranges, optional group selection,
/// and current AnalyseToken (null = Original view, otherwise a scenario token).
/// Enables targeted sending of notifications to connections based on date, group and scenario.
/// Range and group are held in separate maps so that a group selection arriving before the
/// registration cannot be overwritten by it, and vice versa, regardless of the arrival order.
/// State is per process; every member completes synchronously behind the asynchronous contract.
/// </summary>
/// <param name="_connectionRanges">Thread-safe map: ConnectionId -> (DateRange, AnalyseToken); a present entry means "registered"</param>
/// <param name="_selectedGroups">Thread-safe map: ConnectionId -> group filter, written independently of the registration</param>

using System.Collections.Concurrent;

namespace Klacks.Api.Infrastructure.Hubs;

public class ConnectionDateRangeTracker : IConnectionDateRangeTracker
{
    private readonly ConcurrentDictionary<string, (DateOnly StartDate, DateOnly EndDate, Guid? AnalyseToken)> _connectionRanges = new();
    private readonly ConcurrentDictionary<string, Guid?> _selectedGroups = new();

    public Task RegisterConnectionAsync(string connectionId, DateOnly startDate, DateOnly endDate, Guid? analyseToken, CancellationToken cancellationToken = default)
    {
        _connectionRanges[connectionId] = (startDate, endDate, analyseToken);

        return Task.CompletedTask;
    }

    public Task UnregisterConnectionAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        _connectionRanges.TryRemove(connectionId, out _);
        _selectedGroups.TryRemove(connectionId, out _);

        return Task.CompletedTask;
    }

    public Task SetSelectedGroupAsync(string connectionId, Guid? selectedGroupId, CancellationToken cancellationToken = default)
    {
        _selectedGroups[connectionId] = selectedGroupId;

        return Task.CompletedTask;
    }

    public Task<ScheduleConnectionSnapshot?> GetConnectionAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GetConnection(connectionId));
    }

    public Task<IReadOnlyList<ScheduleConnectionSnapshot>> GetConnectionsForDateRangeAsync(DateOnly startDate, DateOnly endDate, Guid? analyseToken, string? excludeConnectionId = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ScheduleConnectionFilter.ForDateRange(GetAllConnections(), startDate, endDate, analyseToken, excludeConnectionId));
    }

    public Task<IReadOnlyList<ScheduleConnectionSnapshot>> GetConnectionsForDatesAsync(IReadOnlyCollection<DateOnly> dates, Guid? analyseToken, string? excludeConnectionId = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ScheduleConnectionFilter.ForDates(GetAllConnections(), dates, analyseToken, excludeConnectionId));
    }

    public Task<GroupedScheduleConnections> GetConnectionsGroupedBySelectedGroupAsync(Guid? analyseToken, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ScheduleConnectionFilter.GroupedBySelectedGroup(GetAllConnections(), analyseToken));
    }

    /// <summary>
    /// Raw, unfiltered local state. Deliberately outside <see cref="IConnectionDateRangeTracker"/>:
    /// application code must go through the filtered members, only infrastructure that merges or
    /// republishes this process's view may read it.
    /// </summary>
    public IReadOnlyList<ScheduleConnectionSnapshot> GetAllConnections()
    {
        var snapshots = new List<ScheduleConnectionSnapshot>(_connectionRanges.Count);

        foreach (var (connectionId, range) in _connectionRanges)
        {
            snapshots.Add(new ScheduleConnectionSnapshot(
                connectionId,
                range.StartDate,
                range.EndDate,
                range.AnalyseToken,
                SelectedGroupOf(connectionId)));
        }

        return snapshots;
    }

    /// <summary>Synchronous single-connection lookup used by the asynchronous contract and by decorators.</summary>
    /// <param name="connectionId">SignalR connection id to look up</param>
    public ScheduleConnectionSnapshot? GetConnection(string connectionId)
    {
        if (!_connectionRanges.TryGetValue(connectionId, out var range))
        {
            return null;
        }

        return new ScheduleConnectionSnapshot(
            connectionId,
            range.StartDate,
            range.EndDate,
            range.AnalyseToken,
            SelectedGroupOf(connectionId));
    }

    private Guid? SelectedGroupOf(string connectionId)
    {
        return _selectedGroups.TryGetValue(connectionId, out var selectedGroupId) ? selectedGroupId : null;
    }
}
