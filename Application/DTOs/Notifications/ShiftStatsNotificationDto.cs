// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Notifications;

public record ShiftStatsNotificationDto
{
    public Guid ShiftId { get; init; }
    public DateTime Date { get; init; }
    public int Engaged { get; init; }
    public string SourceConnectionId { get; init; } = string.Empty;
}
