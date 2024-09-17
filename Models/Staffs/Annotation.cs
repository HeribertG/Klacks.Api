using Klacks_api.Datas;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Klacks_api.Models.Staffs;

public class Annotation : BaseEntity
{
  [Key]
  public Guid Id { get; set; }

  [Required]
  [ForeignKey("Client")]
  public Guid ClientId { get; set; }

  public Client? Client { get; set; }

  public string Note { get; set; } = string.Empty;



}
