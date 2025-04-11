using System.Collections.ObjectModel;

namespace Klacks.Api.Resources.Associations;

public class GroupResource
{
    public GroupResource()
    {
        GroupItems = new Collection<GroupItemResource>();
    }

    public string Description { get; set; } = string.Empty;

    public ICollection<GroupItemResource> GroupItems { get; set; }

    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime ValidFrom { get; set; }

    public DateTime? ValidUntil { get; set; }

    public Guid? Parent { get; set; }

    public Guid? Root { get; set; }

    public int Lft { get; set; }
    public int rgt { get; set; }
}
