// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Notifications;

public record PeriodHoursNotificationDto
{
    public Guid ClientId { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public decimal Hours { get; init; }
    public decimal Surcharges { get; init; }
    public decimal GuaranteedHours { get; init; }
    public string SourceConnectionId { get; init; } = string.Empty;
}
