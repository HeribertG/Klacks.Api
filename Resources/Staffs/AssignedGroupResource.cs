namespace Klacks.Api.Resources.Staffs;

public class AssignedGroupResource
{
    public Guid Id { get; set; }

    public Guid ClientId { get; set; }

    public Guid GroupId { get; set; }

    public virtual GroupResource Group { get; set; } = null!;
}
