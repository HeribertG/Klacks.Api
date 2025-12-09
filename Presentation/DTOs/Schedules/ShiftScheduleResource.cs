namespace Klacks.Api.Presentation.DTOs.Schedules;

public class ShiftScheduleResource
{
    public Guid ShiftId { get; set; }

    public DateOnly Date { get; set; }

    public int DayOfWeek { get; set; }

    public string ShiftName { get; set; } = string.Empty;
}
