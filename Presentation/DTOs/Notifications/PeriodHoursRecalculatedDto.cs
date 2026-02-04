namespace Klacks.Api.Presentation.DTOs.Notifications;

public record PeriodHoursRecalculatedDto
{
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
}
