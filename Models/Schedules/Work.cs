using Klacks_api.Datas;
using Klacks_api.Models.Staffs;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Klacks_api.Models.Schedules;

public class Work : BaseEntity
{

  [JsonIgnore]
  public Client? Client { get; set; }

  [Required]
  [ForeignKey("Clients")]
  public Guid ClientId { get; set; }

  public DateTime From { get; set; }

  public string? Information { get; set; }

  public bool IsSealed { get; set; }

  [JsonIgnore]
  public Shift? Shift { get; set; }

  [Required]
  [ForeignKey("Shift")]
  public Guid ShiftId { get; set; }

  public DateTime Until { get; set; }
}
