using Klacks_api.Datas;
using Klacks_api.Models.Staffs;
using System.ComponentModel.DataAnnotations;

namespace Klacks_api.Models.Associations;

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
