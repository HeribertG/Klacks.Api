using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Klacks_api.Datas;


public class BaseEntity
{

  [Key]
  public Guid Id { get; set; }

  [NotMapped]
  public DateTime? CreateTime { get; set; }
  [NotMapped]
  public string? CurrentUserCreated { get; set; } = string.Empty;
  [NotMapped]
  public string? CurrentUserDeleted { get; set; } = string.Empty;
  [NotMapped]
  public string? CurrentUserUpdated { get; set; } = string.Empty;
  [NotMapped]
  public DateTime? DeletedTime { get; set; }
  [NotMapped]
  public bool IsDeleted { get; set; }
  [NotMapped]
  public DateTime? UpdateTime { get; set; }
}
