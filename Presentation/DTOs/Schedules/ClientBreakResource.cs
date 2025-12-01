using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Presentation.DTOs.Associations;
using System.Collections.ObjectModel;

namespace Klacks.Api.Presentation.DTOs.Schedules;

public class ClientBreakResource
{
    public ClientBreakResource()
    {
        Breaks = new Collection<Break>();
    }

    public ICollection<Break> Breaks { get; set; }

    public string? Company { get; set; } = string.Empty;

    public string? FirstName { get; set; } = string.Empty;

    public GenderEnum Gender { get; set; }

    public Guid Id { get; set; }

    public int IdNumber { get; set; }

    public bool LegalEntity { get; set; }

    public string? MaidenName { get; set; } = string.Empty;

    public MembershipResource? Membership { get; set; }

    public string? Name { get; set; } = string.Empty;

    public string? SecondName { get; set; } = string.Empty;

    public string? Title { get; set; } = string.Empty;

    public int Type { get; set; }
}
