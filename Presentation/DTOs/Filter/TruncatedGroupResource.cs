using Klacks.Api.Presentation.DTOs.Associations;

namespace Klacks.Api.Presentation.DTOs.Filter;

public class TruncatedGroupResource : BaseTruncatedResult
{
    public ICollection<GroupResource> Groups { get; set; } = null!;
}
