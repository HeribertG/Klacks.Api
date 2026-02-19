using System.Text.Json.Serialization;
using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Assistant;

public class AgentSessionMessage : BaseEntity
{
    public Guid SessionId { get; set; }

    public string Role { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public int? TokenCount { get; set; }

    public string? ModelId { get; set; }

    public string? FunctionCalls { get; set; }

    public bool IsCompacted { get; set; }

    public Guid? CompactedIntoId { get; set; }

    [JsonIgnore]
    public virtual AgentSession Session { get; set; } = null!;

    [JsonIgnore]
    public virtual AgentSessionMessage? CompactedInto { get; set; }
}
