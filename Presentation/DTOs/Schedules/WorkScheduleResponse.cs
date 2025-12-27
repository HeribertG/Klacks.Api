namespace Klacks.Api.Presentation.DTOs.Schedules;

public class WorkScheduleResponse
{
    public List<WorkScheduleResource> Entries { get; set; } = new();
    public List<WorkScheduleClientResource> Clients { get; set; } = new();
    public Dictionary<Guid, MonthlyHoursResource> MonthlyHours { get; set; } = new();
}
