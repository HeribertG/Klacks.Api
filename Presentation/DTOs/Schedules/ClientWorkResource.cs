using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Presentation.DTOs.Associations;
using System.Collections.ObjectModel;

namespace Klacks.Api.Presentation.DTOs.Schedules;

public class ClientWorkResource
{
    public ClientWorkResource()
    {
        Works = new Collection<Work>();
    }

    public string? Company { get; set; } = string.Empty;

    public string? FirstName { get; set; } = string.Empty;

    public GenderEnum Gender { get; set; }

    public Guid Id { get; set; }

    public int IdNumber { get; set; }

    public bool LegalEntity { get; set; }

    public string? MaidenName { get; set; } = string.Empty;

    public MembershipResource? Membership { get; set; }

    public Guid MembershipId { get; set; }

    public string? Name { get; set; } = string.Empty;

    public int NeededRows { get; set; } = 3;

    public string? SecondName { get; set; } = string.Empty;

    public string? Title { get; set; } = string.Empty;

    public int Type { get; set; }

    public ICollection<Work> Works { get; set; }
}
