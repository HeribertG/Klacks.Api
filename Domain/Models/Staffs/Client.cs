using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Models.Staffs;

public class Client : BaseEntity
{
    public Client()
    {
        Addresses = new Collection<Address>();
        Communications = new Collection<Communication>();
        Annotations = new Collection<Annotation>();
        Breaks = new Collection<Break>();
        Works = new Collection<Work>();
        GroupItems = new Collection<GroupItem>();
        ClientContracts = new Collection<ClientContract>();
    }

    public ClientImage? ClientImage { get; set; }

    public ICollection<Address> Addresses { get; set; }

    public ICollection<Annotation> Annotations { get; set; }

    [DataType(DataType.Date)]
    public DateTime? Birthdate { get; set; }

    [JsonIgnore]
    public ICollection<Break> Breaks { get; set; }

    public ICollection<ClientContract> ClientContracts { get; set; }

    [JsonIgnore]
    public ICollection<GroupItem> GroupItems { get; set; }

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
    public EntityTypeEnum Type { get; set; }

    public ICollection<Work> Works { get; set; }
}
