namespace Klacks.Api.Presentation.DTOs.Schedules;

public class WorkResource : ScheduleEntryResource
{
    public ShiftResource? Shift { get; set; }

    public Guid ShiftId { get; set; }
}
