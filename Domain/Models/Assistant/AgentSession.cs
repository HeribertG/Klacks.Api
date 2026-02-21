using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Models.Assistant;

public class AgentSession : BaseEntity
{
    public Guid AgentId { get; set; }

    public string SessionId { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public string? Title { get; set; }

    public string? Summary { get; set; }

    public string Status { get; set; } = AgentSessionStatus.Active;

    public int MessageCount { get; set; }

    public int TokenCountEst { get; set; }

    public int CompactionCount { get; set; }

    public string ActiveCategories { get; set; } = "[]";

    public string Channel { get; set; } = "web";

    public DateTime LastMessageAt { get; set; }

    public string? LastModelId { get; set; }

    public bool IsArchived { get; set; }

    public virtual Agent Agent { get; set; } = null!;

    public virtual ICollection<AgentSessionMessage> Messages { get; set; } = new List<AgentSessionMessage>();
}
