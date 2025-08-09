namespace Klacks.Api.Presentation.DTOs.Associations;

public class GroupTreeResource
{
    public Guid? RootId { get; set; }

    public List<GroupResource> Nodes { get; set; } = new List<GroupResource>();
}
