namespace Klacks.Api.Application.DTOs.Notifications;

public record ScheduleChangeNotificationDto
{
    public Guid ClientId { get; init; }
    public DateOnly ChangeDate { get; init; }
    public string SourceConnectionId { get; init; } = string.Empty;
}
