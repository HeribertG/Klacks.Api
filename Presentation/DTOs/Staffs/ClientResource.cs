using Klacks.Api.Domain.Enums;
using Klacks.Api.Presentation.DTOs.Associations;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Presentation.DTOs.Settings;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Presentation.DTOs.Staffs;

public class ClientResource
{
    public ClientResource()
    {
        Addresses = new Collection<AddressResource>();
        Communications = new Collection<CommunicationResource>();
        Annotations = new Collection<AnnotationResource>();
        Works = new Collection<WorkResource>();
    }

    public ICollection<AddressResource> Addresses { get; set; }

    public ICollection<AnnotationResource> Annotations { get; set; }

    [DataType(DataType.Date)]
    public DateTime? Birthdate { get; set; }

    public ICollection<CommunicationResource> Communications { get; set; }

    public string? Company { get; set; } = string.Empty;

    public string? FirstName { get; set; } = string.Empty;

    [Required]
    public GenderEnum Gender { get; set; }

    public Guid Id { get; set; }

    public int IdNumber { get; set; }

    public bool LegalEntity { get; set; }

    public string? MaidenName { get; set; } = string.Empty;

    public MembershipResource? Membership { get; set; }

    public Guid MembershipId { get; set; }

    public string? Name { get; set; } = string.Empty;

    public string? PasswortResetToken { get; set; } = string.Empty;

    public string? SecondName { get; set; } = string.Empty;

    public string? Title { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }

    [DefaultValue(0)]
    public int Type { get; set; }

    public ICollection<WorkResource> Works { get; set; }
}
