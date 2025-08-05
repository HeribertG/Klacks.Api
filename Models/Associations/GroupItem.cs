using Klacks.Api.Datas;
using Klacks.Api.Models.Schedules;
using Klacks.Api.Models.Staffs;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Klacks.Api.Models.Associations;

public class GroupItem : BaseEntity
{
    [ForeignKey("Client")]
    public Guid? ClientId { get; set; }

    [ForeignKey("Shift")]
    public Guid? ShiftId { get; set; }

    [Required]
    [ForeignKey("Group")]
    public Guid GroupId { get; set; }

    public virtual Client? Client { get; set; } = null!;

    public virtual Shift? Shift { get; set; } = null!;

    public virtual Group? Group { get; set; }
}
