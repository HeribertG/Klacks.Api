using Klacks.Api.Resources.Staffs;

namespace Klacks.Api.Resources.Associations;

/// <summary>
/// Resource für einen Knoten im Gruppenbaum
/// </summary>
public class GroupTreeNodeResource
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime ValidFrom { get; set; }

    public DateTime? ValidUntil { get; set; }

    public Guid? ParentId { get; set; }

    public Guid? Root { get; set; }

    public int Lft { get; set; }

    public int Rgt { get; set; }

    public int Depth { get; set; }

    public int ClientsCount { get; set; }

    public ICollection<ClientResource>? Clients { get; set; }

    public DateTime? CreateTime { get; set; }

    public DateTime? UpdateTime { get; set; }

    public string? CurrentUserCreated { get; set; }

    public string? CurrentUserUpdated { get; set; }
}
