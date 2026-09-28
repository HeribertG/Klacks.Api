// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Notifications;

public record CollisionListNotificationDto
{
    public List<CollisionNotificationDto> Collisions { get; init; } = [];
    public bool IsFullRefresh { get; init; }
    public Guid? CheckedClientId { get; init; }
    public DateOnly? CheckedDate { get; init; }
    public Guid? AnalyseToken { get; init; }
}
