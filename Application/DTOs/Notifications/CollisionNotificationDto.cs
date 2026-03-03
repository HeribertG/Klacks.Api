// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Notifications;

public record CollisionNotificationDto
{
    public Guid WorkId1 { get; init; }
    public Guid WorkId2 { get; init; }
    public Guid ClientId { get; init; }
    public string ClientName { get; init; } = string.Empty;
    public DateOnly Date { get; init; }
    public string TimeRange1 { get; init; } = string.Empty;
    public string TimeRange2 { get; init; } = string.Empty;
}
