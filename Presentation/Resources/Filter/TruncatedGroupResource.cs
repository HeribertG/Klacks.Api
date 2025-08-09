namespace Klacks.Api.Presentation.Resources.Filter;

public class TruncatedGroupResource : BaseTruncatedResult
{
    public ICollection<GroupResource> Groups { get; set; } = null!;
}
