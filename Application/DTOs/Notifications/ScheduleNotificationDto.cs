namespace Klacks.Api.Application.DTOs.Notifications;

public record ScheduleNotificationDto
{
    public Guid ClientId { get; init; }
    public DateOnly CurrentDate { get; init; }
    public DateOnly PeriodStartDate { get; init; }
    public DateOnly PeriodEndDate { get; init; }
    public string OperationType { get; init; } = string.Empty;
    public string SourceConnectionId { get; init; } = string.Empty;
}
