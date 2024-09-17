using Klacks_api.Datas;
using Klacks_api.Models.Staffs;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Klacks_api.Models.Associations;

public class Membership : BaseEntity
{
  [JsonIgnore]
  public Client? Client { get; set; }

  [Required]
  [ForeignKey("Clients")]
  public Guid ClientId { get; set; }

  public int Type { get; set; }

  [Required]
  [DataType(DataType.Date)]
  public DateTime ValidFrom { get; set; }

  [DataType(DataType.Date)]
  public DateTime? ValidUntil { get; set; }
}
