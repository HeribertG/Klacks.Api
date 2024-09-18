using Klacks_api.Datas;
using Klacks_api.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Klacks_api.Models.Staffs;

public class Communication : BaseEntity
{

   public Guid ClientId { get; set; }

  public virtual Client Client { get; set; } = null!;   

  [Required]
  public CommunicationTypeEnum Type { get; set; }


  [StringLength(100)]
  public string Value { get; set; } = String.Empty;

  public string Prefix { get; set; } = String.Empty;

  public string Description { get; set; } = String.Empty;


}
