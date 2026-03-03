// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Notifications;

public record WorkNotificationDto
{
    public Guid WorkId { get; init; }
    public Guid ClientId { get; init; }
    public Guid ShiftId { get; init; }
    public DateOnly CurrentDate { get; init; }
    public DateOnly PeriodStartDate { get; init; }
    public DateOnly PeriodEndDate { get; init; }
    public string OperationType { get; init; } = string.Empty;
    public string SourceConnectionId { get; init; } = string.Empty;
}
