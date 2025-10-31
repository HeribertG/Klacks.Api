using Klacks.Api.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Klacks.Api.Domain.Models.Schedules;

public class ContainerTemplateItem : BaseEntity
{
    [ForeignKey("ContainerTemplate")]
    public Guid ContainerTemplateId { get; set; }

    [ForeignKey("Shift")]
    public Guid ShiftId { get; set; }

    public virtual ContainerTemplate ContainerTemplate { get; set; } = null!;

    public virtual Shift Shift { get; set; } = null!;
}
