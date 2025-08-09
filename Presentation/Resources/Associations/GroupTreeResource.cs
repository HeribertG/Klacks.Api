namespace Klacks.Api.Presentation.Resources.Associations;

public class GroupTreeResource
{
    public Guid? RootId { get; set; }

    public List<GroupResource> Nodes { get; set; } = new List<GroupResource>();
}
