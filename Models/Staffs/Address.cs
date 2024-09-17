using Klacks_api.Datas;
using Klacks_api.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Klacks_api.Models.Staffs;

public class Address : BaseEntity
{

  [Key]
  public Guid Id { get; set; }

  [Required]
  [ForeignKey("Client")]
  public Guid ClientId { get; set; }

  public Client? Client { get; set; }


  [Required]
  [DataType(DataType.Date)]
  public DateTime? ValidFrom { get; set; }



  [Required]
  public AddressTypeEnum Type { get; set; }
  public string AddressLine1 { get; set; } = string.Empty;
  public string AddressLine2 { get; set; } = string.Empty;
  public string Street { get; set; } = string.Empty;
  public string Street2 { get; set; } = string.Empty;
  public string Street3 { get; set; } = string.Empty;
  public string Zip { get; set; } = string.Empty;
  public string City { get; set; } = string.Empty;
  public string State { get; set; } = string.Empty;
  public string Country { get; set; } = string.Empty;


}
