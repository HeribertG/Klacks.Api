namespace Klacks.Api.Application.DTOs.Schedules;

public class WorkScheduleResponse
{
    public List<WorkScheduleResource> Entries { get; set; } = new();
    public List<WorkScheduleClientResource> Clients { get; set; } = new();
    public Dictionary<Guid, PeriodHoursResource> PeriodHours { get; set; } = new();
    public int TotalClientCount { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}
