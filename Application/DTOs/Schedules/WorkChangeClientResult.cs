namespace Klacks.Api.Application.DTOs.Schedules;

public class WorkChangeClientResult
{
    public Guid ClientId { get; set; }

    public PeriodHoursResource? PeriodHours { get; set; }

    public List<WorkScheduleResource> ScheduleEntries { get; set; } = [];
}
