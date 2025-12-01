using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Staffs;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Models.Associations;

public class GroupItem : BaseEntity
{
    [ForeignKey("Employee")]
    public Guid? ClientId { get; set; }

    [ForeignKey("Shift")]
    public Guid? ShiftId { get; set; }

    [Required]
    [ForeignKey("Group")]
    public Guid GroupId { get; set; }

    [DataType(DataType.Date)]
    public DateTime? ValidFrom { get; set; }

    [DataType(DataType.Date)]
    public DateTime? ValidUntil { get; set; }

    [JsonIgnore]
    public virtual Client? Client { get; set; } = null!;

    [JsonIgnore]
    public virtual Shift? Shift { get; set; } = null!;

    [JsonIgnore]
    public virtual Group? Group { get; set; }
}
