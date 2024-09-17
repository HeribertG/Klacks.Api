using System.ComponentModel.DataAnnotations;

namespace Klacks_api.Models.Settings
{
  public class PostcodeCH
  {
    public string City { get; set; } = string.Empty;

    [Key]
    public int Id { get; set; }

    [StringLength(10)]
    public string State { get; set; } = string.Empty;

    public int Zip { get; set; }
  }
}
