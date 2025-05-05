namespace Klacks.Api.Resources.Associations;

/// <summary>
/// Resource für die Baumdarstellung einer Gruppe
/// </summary>
public class GroupTreeResource
{
    public Guid? RootId { get; set; }

    public List<GroupResource> Nodes { get; set; } = new List<GroupResource>();
}
