using Klacks_api.Resources.Associations;

namespace Klacks_api.Resources.Filter
{
  public class TruncatedGroupResource : BaseTruncatedResult
  {
    public ICollection<GroupResource> Groups { get; set; } = null!;
  }
}
