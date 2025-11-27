using Klacks.Api.Domain.Enums;

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

    public string? StartBase { get; set; }

    public string? EndBase { get; set; }

    public RouteInfoResource? RouteInfo { get; set; }

    public ContainerTransportMode TransportMode { get; set; } = ContainerTransportMode.ByCar;

    public ShiftResource? Shift { get; set; }

    public List<ContainerTemplateItemResource> ContainerTemplateItems { get; set; } = new List<ContainerTemplateItemResource>();
}
