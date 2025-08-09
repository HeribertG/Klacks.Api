using Klacks.Api.Models.Associations;

namespace Klacks.Api.Presentation.Resources.Filter
{
    public class TruncatedGroup : BaseTruncatedResult
    {
        public ICollection<Group> Groups { get; set; } = null!;
    }
}
