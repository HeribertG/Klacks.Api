using Klacks_api.Datas;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Klacks_api.Models.Associations;

public class Contract : BaseEntity
{
  public decimal GuaranteedHoursPerMonth { get; set; }

  public decimal MaximumHoursPerMonth { get; set; }

  [JsonIgnore]
  public Membership? Membership { get; set; }

  [Required]
  [ForeignKey("Membership")]
  public Guid MembershipId { get; set; }

  [Required]
  [DataType(DataType.Date)]
  public DateTime ValidFrom { get; set; }

  [DataType(DataType.Date)]
  public DateTime? ValidUntil { get; set; }
}
