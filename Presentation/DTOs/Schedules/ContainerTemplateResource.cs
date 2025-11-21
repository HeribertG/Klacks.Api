namespace Klacks.Api.Presentation.DTOs.Schedules;

public class ContainerTemplateResource
{
    public Guid Id { get; set; }

    public Guid ContainerId { get; set; }

    public TimeOnly FromTime { get; set; }

    public TimeOnly UntilTime { get; set; }

    public int Weekday { get; set; }

    public bool IsWeekdayOrHoliday { get; set; }

    public bool IsHoliday { get; set; }

    public ShiftResource? Shift { get; set; }

    public List<ContainerTemplateItemResource> ContainerTemplateItems { get; set; } = new List<ContainerTemplateItemResource>();
}
