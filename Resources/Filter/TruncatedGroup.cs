using Klacks.Api.Models.Associations;

namespace Klacks.Api.Resources.Filter
{
    public class TruncatedGroup : BaseTruncatedResult
    {
        public ICollection<Group> Groups { get; set; } = null!;
    }
}
