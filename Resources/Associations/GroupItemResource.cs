using Klacks_api.Resources.Staffs;

namespace Klacks_api.Resources.Associations
{
  public class GroupItemResource
  {
    public ClientResource? Client { get; set; }

    public Guid ClientId { get; set; }

    public Guid GroupId { get; set; }

    public Guid Id { get; set; }
  }
}
