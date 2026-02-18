namespace Klacks.Api.Application.DTOs.Schedules;

public class ScheduleChangeResource
{
    public Guid ClientId { get; set; }
    public DateOnly ChangeDate { get; set; }
}
