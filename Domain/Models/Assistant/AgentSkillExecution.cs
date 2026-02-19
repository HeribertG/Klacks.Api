using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Assistant;

public class AgentSkillExecution : BaseEntity
{
    public Guid AgentId { get; set; }

    public Guid SkillId { get; set; }

    public Guid SessionId { get; set; }

    public string? UserId { get; set; }

    public string ToolName { get; set; } = string.Empty;

    public string? ParametersJson { get; set; }

    public bool Success { get; set; }

    public string? ResultMessage { get; set; }

    public string? ErrorMessage { get; set; }

    public int DurationMs { get; set; }

    public string TriggeredBy { get; set; } = "agent";

    public virtual AgentSkill Skill { get; set; } = null!;
}
