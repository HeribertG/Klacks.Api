using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace Klacks.Api.Domain.Models.Schedules;

public class ContainerTemplate : BaseEntity
{
    [ForeignKey("Shift")]
    public Guid ContainerId { get; set; }

    public TimeOnly FromTime { get; set; }

    public TimeOnly UntilTime { get; set; }

    public int Weekday { get; set; }

    public bool IsWeekdayOrHoliday { get; set; }

    public bool IsHoliday { get; set; }

    public string? StartBase { get; set; }

    public string? EndBase { get; set; }

    [Column(TypeName = "jsonb")]
    public RouteInfo? RouteInfo { get; set; }

    public ContainerTransportMode TransportMode { get; set; } = ContainerTransportMode.ByCar;

    public virtual Shift Shift { get; set; } = null!;

    public List<ContainerTemplateItem> ContainerTemplateItems { get; set; } = new List<ContainerTemplateItem>();
}
