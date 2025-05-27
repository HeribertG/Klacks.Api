namespace Klacks.Api.Resources.Filter;

public class TruncatedGroupResource : BaseTruncatedResult
{
public ICollection<GroupResource> Groups { get; set; } = null!;
}
