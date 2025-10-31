using Klacks.Api.Domain.Common;
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

    public virtual Shift Shift { get; set; } = null!;

    public List<ContainerTemplateItem> Items { get; set; } = new List<ContainerTemplateItem>();
}
