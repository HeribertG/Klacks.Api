namespace Klacks.Api.Presentation.DTOs.Notifications;

public record PeriodHoursNotificationDto
{
    public Guid ClientId { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public decimal Hours { get; init; }
    public decimal Surcharges { get; init; }
    public string SourceConnectionId { get; init; } = string.Empty;
}
