namespace Klacks.Api.Domain.Models.Assistant;

public class AgentMemoryTag
{
    public Guid MemoryId { get; set; }

    public string Tag { get; set; } = string.Empty;

    public virtual AgentMemory Memory { get; set; } = null!;
}
