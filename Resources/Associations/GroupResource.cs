using Klacks.Api.Resources.Associations;
using System.Collections.ObjectModel;

public class GroupResource
{
    public GroupResource()
    {
        GroupItems = new Collection<GroupItemResource>();
        Children = new List<GroupResource>();
    }

    public string Description { get; set; } = string.Empty;

    public ICollection<GroupItemResource> GroupItems { get; set; }
    public List<GroupResource> Children { get; set; }

    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime ValidFrom { get; set; }

    public DateTime? ValidUntil { get; set; }

    public Guid? Parent { get; set; }

    public Guid? Root { get; set; }

    public int Lft { get; set; }

    public int Rgt { get; set; }
 
    public int Depth { get; set; }

    public int ClientsCount
    {
        get
        {
            return GroupItems?.Count ?? 0;
        }
    }
}