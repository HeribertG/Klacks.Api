using Klacks_api.Models.Associations;

namespace Klacks_api.Resources.Filter
{
  public class TruncatedGroup : BaseTruncatedResult
  {
    public ICollection<Group> Groups { get; set; } = null!;
  }
}
