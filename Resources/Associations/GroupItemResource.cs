using Klacks.Api.Resources.Staffs;

namespace Klacks.Api.Resources.Associations;

public class GroupItemResource
{
public ClientResource? Client { get; set; }

public Guid ClientId { get; set; }

public Guid GroupId { get; set; }

public Guid Id { get; set; }
}
