// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Infrastructure.Hubs;

/// <summary>
/// The single place that decides which schedule connections a notification reaches: scenario token
/// equality, date-range overlap, sender exclusion and group partitioning. Kept free of dependencies so
/// the process-local tracker and any later distributed tracker run the same code, not merely the same
/// algorithm - a second copy of this predicate would be a visibility bug waiting to happen.
/// </summary>
public static class ScheduleConnectionFilter
{
    /// <summary>Connections of the given scenario whose date range overlaps [startDate, endDate].</summary>
    /// <param name="connections">Unfiltered connection snapshots to select from</param>
    /// <param name="startDate">First day of the notified range</param>
    /// <param name="endDate">Last day of the notified range</param>
    /// <param name="analyseToken">Scenario partition key that has to match exactly</param>
    /// <param name="excludeConnectionId">Connection to leave out, usually the sender</param>
    public static IReadOnlyList<ScheduleConnectionSnapshot> ForDateRange(
        IReadOnlyList<ScheduleConnectionSnapshot> connections,
        DateOnly startDate,
        DateOnly endDate,
        Guid? analyseToken,
        string? excludeConnectionId = null)
    {
        var matches = new List<ScheduleConnectionSnapshot>();

        foreach (var connection in connections)
        {
            if (!IsCandidate(connection, analyseToken, excludeConnectionId))
            {
                continue;
            }

            if (RangesOverlap(connection.Start, connection.End, startDate, endDate))
            {
                matches.Add(connection);
            }
        }

        return matches;
    }

    /// <summary>Connections of the given scenario whose date range contains at least one of the dates.</summary>
    /// <param name="connections">Unfiltered connection snapshots to select from</param>
    /// <param name="dates">Days the notification refers to</param>
    /// <param name="analyseToken">Scenario partition key that has to match exactly</param>
    /// <param name="excludeConnectionId">Connection to leave out, usually the sender</param>
    public static IReadOnlyList<ScheduleConnectionSnapshot> ForDates(
        IReadOnlyList<ScheduleConnectionSnapshot> connections,
        IReadOnlyCollection<DateOnly> dates,
        Guid? analyseToken,
        string? excludeConnectionId = null)
    {
        var matches = new List<ScheduleConnectionSnapshot>();

        foreach (var connection in connections)
        {
            if (!IsCandidate(connection, analyseToken, excludeConnectionId))
            {
                continue;
            }

            foreach (var date in dates)
            {
                if (RangesOverlap(connection.Start, connection.End, date, date))
                {
                    matches.Add(connection);
                    break;
                }
            }
        }

        return matches;
    }

    /// <summary>Connections of the given scenario split into those without and those with a group filter.</summary>
    /// <param name="connections">Unfiltered connection snapshots to select from</param>
    /// <param name="analyseToken">Scenario partition key that has to match exactly</param>
    public static GroupedScheduleConnections GroupedBySelectedGroup(
        IReadOnlyList<ScheduleConnectionSnapshot> connections,
        Guid? analyseToken)
    {
        var ungrouped = new List<ScheduleConnectionSnapshot>();
        var byGroup = new Dictionary<Guid, List<ScheduleConnectionSnapshot>>();

        foreach (var connection in connections)
        {
            if (connection.AnalyseToken != analyseToken)
            {
                continue;
            }

            if (connection.SelectedGroupId == null)
            {
                ungrouped.Add(connection);
                continue;
            }

            if (!byGroup.TryGetValue(connection.SelectedGroupId.Value, out var list))
            {
                list = [];
                byGroup[connection.SelectedGroupId.Value] = list;
            }

            list.Add(connection);
        }

        return new GroupedScheduleConnections(
            ungrouped,
            byGroup.ToDictionary(entry => entry.Key, entry => (IReadOnlyList<ScheduleConnectionSnapshot>)entry.Value));
    }

    private static bool IsCandidate(ScheduleConnectionSnapshot connection, Guid? analyseToken, string? excludeConnectionId)
    {
        if (excludeConnectionId != null && connection.ConnectionId == excludeConnectionId)
        {
            return false;
        }

        return connection.AnalyseToken == analyseToken;
    }

    private static bool RangesOverlap(DateOnly aStart, DateOnly aEnd, DateOnly bStart, DateOnly bEnd)
    {
        return aStart <= bEnd && bStart <= aEnd;
    }
}
