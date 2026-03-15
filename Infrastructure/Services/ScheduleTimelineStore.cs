// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// In-Memory-Store für ClientTimelines. Verwaltet alle Timelines als ScheduleBoard.
/// </summary>
using System.Collections.Concurrent;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Application.Interfaces;

namespace Klacks.Api.Infrastructure.Services;

public class ScheduleTimelineStore : IScheduleTimelineStore
{
    private readonly ConcurrentDictionary<Guid, ClientTimeline> _timelines = new();

    public void SetTimeline(Guid clientId, ClientTimeline timeline)
    {
        _timelines[clientId] = timeline;
    }

    public void RemoveTimeline(Guid clientId)
    {
        _timelines.TryRemove(clientId, out _);
    }

    public ClientTimeline? GetTimeline(Guid clientId)
    {
        return _timelines.TryGetValue(clientId, out var timeline) ? timeline : null;
    }

    public ScheduleBoard GetBoard()
    {
        var board = new ScheduleBoard();
        foreach (var (clientId, timeline) in _timelines)
        {
            board.SetTimeline(clientId, timeline);
        }
        return board;
    }
}
