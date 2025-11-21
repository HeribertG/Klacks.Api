namespace Klacks.Api.Presentation.DTOs.Schedules;

public class ContainerTemplateItemResource
{
    public Guid Id { get; set; }

    public Guid ContainerTemplateId { get; set; }

    public Guid ShiftId { get; set; }

    public int Weekday { get; set; }

    public TimeOnly? StartShift { get; set; }

    public TimeOnly? EndShift { get; set; }

    public TimeOnly BriefingTime { get; set; }

    public TimeOnly DebriefingTime { get; set; }

    public TimeOnly TravelTimeAfter { get; set; }

    public TimeOnly TravelTimeBefore { get; set; }

    public TimeOnly? TimeRangeStartShift { get; set; }

    public TimeOnly? TimeRangeEndShift { get; set; }

    public ShiftResource? Shift { get; set; }
}
