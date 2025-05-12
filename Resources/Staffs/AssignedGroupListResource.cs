using Klacks.Api.Resources.Associations;

namespace Klacks.Api.Resources.Staffs;

public class AssignedGroupListResource
{
    public List<AssignedGroupResource> Nodes { get; set; } = new List<AssignedGroupResource>();
}
