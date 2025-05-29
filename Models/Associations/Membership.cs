using Klacks.Api.Datas;
using Klacks.Api.Models.Staffs;
using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Models.Associations;

public class Membership : BaseEntity
{
    public virtual Client Client { get; set; } = null!;

    public Guid ClientId { get; set; }

    public int Type { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime ValidFrom { get; set; }

    [DataType(DataType.Date)]
    public DateTime? ValidUntil { get; set; }
}
