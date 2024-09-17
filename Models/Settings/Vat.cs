using System.ComponentModel.DataAnnotations;

namespace Klacks_api.Models.Settings
{
  public class Vat
  {
    [Key]
    public Guid Id { get; set; }

    public decimal VATRate { get; set; }

    public bool IsDefault { get; set; }
  }
}
