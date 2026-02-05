namespace Klacks.Api.Presentation.DTOs.Schedules;

public class ShiftScheduleResponse
{
    public List<ShiftScheduleResource> Shifts { get; set; } = new();

    public int TotalCount { get; set; }
}
