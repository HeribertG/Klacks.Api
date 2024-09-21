using Klacks.Api.Datas;
using Klacks.Api.Enums;
using Klacks.Api.Models.Associations;
using Klacks.Api.Models.Schedules;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Models.Staffs;

public class Client : BaseEntity
{
  public Client()
  {
    Addresses = new Collection<Address>();
    Communications = new Collection<Communication>();
    Annotations = new Collection<Annotation>();
    Breaks = new Collection<Break>();
    Works = new Collection<Work>();
  }

  public ICollection<Address> Addresses { get; set; }

  public ICollection<Annotation> Annotations { get; set; }

  [DataType(DataType.Date)]
  public DateTime? Birthdate { get; set; }

  public ICollection<Break> Breaks { get; set; }

  public ICollection<Communication> Communications { get; set; }

  [StringLength(100)]
  public string? Company { get; set; } = string.Empty;

  [StringLength(100)]
  public string? FirstName { get; set; } = string.Empty;

  [Required]
  public GenderEnum Gender { get; set; }

  public int IdNumber { get; set; }

  public bool LegalEntity { get; set; }

  [StringLength(100)]
  public string? MaidenName { get; set; } = string.Empty;

  public Membership? Membership { get; set; }

  public Guid MembershipId { get; set; }

  [StringLength(100)]
  public string Name { get; set; } = string.Empty;

  public string? PasswortResetToken { get; set; } = string.Empty;

  [StringLength(100)]
  public string? SecondName { get; set; } = string.Empty;

  [StringLength(100)]
  public string? Title { get; set; } = string.Empty;

  [DefaultValue(0)]
  public int Type { get; set; }

  public ICollection<Work> Works { get; set; }
}
