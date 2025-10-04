using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Models.Staffs;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Klacks.Api.Domain.Models.Associations;

public class Membership : BaseEntity
{
    public virtual Client Client { get; set; } = null!;

    [Required]
    [ForeignKey("Employee")]
    public Guid ClientId { get; set; }

    public int Type { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime ValidFrom { get; set; }

    [DataType(DataType.Date)]
    public DateTime? ValidUntil { get; set; }
}
