using Klacks_api.Datas;
using Klacks_api.Models.Staffs;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Klacks_api.Models.Schedules;

public class Break : BaseEntity
{
 
  [JsonIgnore]
  public Absence? Absence { get; set; }

  [Required]
  [ForeignKey("Absences")]
  public Guid AbsenceId { get; set; }

  [JsonIgnore]
  public Client? Client { get; set; }

  [Required]
  [ForeignKey("Clients")]
  public Guid ClientId { get; set; }

  public DateTime From { get; set; }

  public string? Information { get; set; }

  public DateTime Until { get; set; }

  [Required]
  [ForeignKey("BreakReason")]
  public Guid BreakReasonId { get; set; }
  public BreakReason? BreakReason { get; set; }
}
