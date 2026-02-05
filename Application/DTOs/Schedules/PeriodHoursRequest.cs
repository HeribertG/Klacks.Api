namespace Klacks.Api.Application.DTOs.Schedules;

public class PeriodHoursRequest
{
    public List<Guid> ClientIds { get; set; } = new();
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}
