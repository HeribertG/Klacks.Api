// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Builds the legacy collision notifications of the live schedule check from a client's timeline (moved out of
/// ScheduleTimelineBackgroundService unchanged, to keep that service within its size ceiling).
/// </summary>

using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Infrastructure.Services;

public static class TimelineCollisionNotificationBuilder
{
    public static CollisionListNotificationDto BuildNotification(
        ClientTimeline timeline,
        Dictionary<Guid, string> clientNameLookup,
        bool isFullRefresh,
        Guid? checkedClientId,
        DateOnly? checkedDate,
        Guid? analyseToken)
    {
        return new CollisionListNotificationDto
        {
            Collisions = BuildList(timeline, clientNameLookup),
            IsFullRefresh = isFullRefresh,
            CheckedClientId = checkedClientId,
            CheckedDate = checkedDate,
            AnalyseToken = analyseToken
        };
    }

    public static List<CollisionNotificationDto> BuildList(
        ClientTimeline timeline,
        Dictionary<Guid, string> clientNameLookup)
    {
        var pairs = timeline.GetCollisions();
        if (pairs.Count == 0) return [];

        clientNameLookup.TryGetValue(timeline.ClientId, out var clientName);
        clientName ??= string.Empty;

        return pairs.Select(p => new CollisionNotificationDto
        {
            WorkId1 = p.A.SourceId,
            WorkId2 = p.B.SourceId,
            ClientId = timeline.ClientId,
            ClientName = clientName,
            Date = p.A.OwnerDate,
            TimeRange1 = $"{p.A.Start:HH:mm} - {p.A.End:HH:mm}",
            TimeRange2 = $"{p.B.Start:HH:mm} - {p.B.End:HH:mm}",
            BlockType1 = p.A.BlockType.ToString(),
            BlockType2 = p.B.BlockType.ToString()
        }).ToList();
    }
}
