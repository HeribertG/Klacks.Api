using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Assistant;

public class AgentMemory : BaseEntity
{
    public Guid AgentId { get; set; }

    public string Category { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public int Importance { get; set; } = 5;

    public float[]? Embedding { get; set; }

    public bool IsPinned { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public Guid? SupersedesId { get; set; }

    public int AccessCount { get; set; }

    public DateTime? LastAccessedAt { get; set; }

    public string Source { get; set; } = "conversation";

    public string? SourceRef { get; set; }

    public string Metadata { get; set; } = "{}";

    public virtual Agent Agent { get; set; } = null!;

    public virtual AgentMemory? Supersedes { get; set; }

    public virtual ICollection<AgentMemoryTag> Tags { get; set; } = new List<AgentMemoryTag>();
}
