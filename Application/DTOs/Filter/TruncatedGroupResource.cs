using Klacks.Api.Application.DTOs.Associations;

namespace Klacks.Api.Application.DTOs.Filter;

public class TruncatedGroupResource : BaseTruncatedResult
{
    public ICollection<GroupResource> Groups { get; set; } = null!;
}
