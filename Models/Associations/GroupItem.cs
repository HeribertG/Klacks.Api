using Klacks_api.Datas;
using Klacks_api.Models.Staffs;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Klacks_api.Models.Associations;

public class GroupItem : BaseEntity
{
  [JsonIgnore]
  public Client? Client { get; set; }

  [Required]
  [ForeignKey("Clients")]
  public Guid ClientId { get; set; }

  [JsonIgnore]
  public Group? Group { get; set; }

  [Required]
  [ForeignKey("Groups")]
  public Guid GroupId { get; set; }
}
