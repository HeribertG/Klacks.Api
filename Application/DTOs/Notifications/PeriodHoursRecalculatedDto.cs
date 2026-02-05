namespace Klacks.Api.Application.DTOs.Notifications;

public record PeriodHoursRecalculatedDto
{
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
}
