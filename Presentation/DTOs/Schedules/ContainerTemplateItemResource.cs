namespace Klacks.Api.Presentation.DTOs.Schedules;

public class ContainerTemplateItemResource
{
    public Guid Id { get; set; }

    public Guid ContainerTemplateId { get; set; }

    public Guid ShiftId { get; set; }

    public int Weekday { get; set; }

    public ShiftResource? Shift { get; set; }
}
