using Klacks.Api.Datas;
using Klacks.Api.Models.Staffs;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Klacks.Api.Models.Associations;

public class Membership : BaseEntity
{
    public virtual Client Client { get; set; } = null!;

    [Required]
    [ForeignKey("Client")]
    public Guid ClientId { get; set; }

    public int Type { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime ValidFrom { get; set; }

    [DataType(DataType.Date)]
    public DateTime? ValidUntil { get; set; }
}
