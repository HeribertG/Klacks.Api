// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Builds the legacy collision notifications of the live schedule check from a client's timeline (moved out of
/// ScheduleTimelineBackgroundService unchanged, to keep that service within its size ceiling). A work over an
/// on-call break is not a collision here (it is reported as an on-call-overlap Warning in the validation list).
/// </summary>
/// <param name="onCallBreakIds">Source ids of the break blocks whose absence type is on-call</param>

using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Schedules;

namespace Klacks.Api.Infrastructure.Services;

public static class TimelineCollisionNotificationBuilder
{
    public static CollisionListNotificationDto BuildNotification(
        ClientTimeline timeline,
        Dictionary<Guid, string> clientNameLookup,
        bool isFullRefresh,
        Guid? checkedClientId,
        DateOnly? checkedDate,
        Guid? analyseToken,
        IReadOnlySet<Guid> onCallBreakIds)
    {
        return new CollisionListNotificationDto
        {
            Collisions = BuildList(timeline, clientNameLookup, onCallBreakIds),
            IsFullRefresh = isFullRefresh,
            CheckedClientId = checkedClientId,
            CheckedDate = checkedDate,
            AnalyseToken = analyseToken
        };
    }

    public static List<CollisionNotificationDto> BuildList(
        ClientTimeline timeline,
        Dictionary<Guid, string> clientNameLookup,
        IReadOnlySet<Guid> onCallBreakIds)
    {
        var pairs = timeline.GetCollisions()
            .Where(p => !OnCallOverlapDetector.TryGet(p.A, p.B, onCallBreakIds, out _, out _))
            .ToList();
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
