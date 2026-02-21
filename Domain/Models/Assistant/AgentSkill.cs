using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Models.Assistant;

public class AgentSkill : BaseEntity
{
    public Guid AgentId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string ParametersJson { get; set; } = "[]";

    public string? RequiredPermission { get; set; }

    public string ExecutionType { get; set; } = LlmExecutionTypes.Skill;

    public string Category { get; set; } = AgentSkillDefaults.Category;

    public bool IsEnabled { get; set; } = true;

    public int SortOrder { get; set; }

    public string HandlerType { get; set; } = AgentSkillDefaults.HandlerType;

    public string HandlerConfig { get; set; } = "{}";

    public string TriggerKeywords { get; set; } = "[]";

    public string AllowedChannels { get; set; } = "[]";

    public int? MaxCallsPerSession { get; set; }

    public bool AlwaysOn { get; set; }

    public int Version { get; set; } = 1;

    public virtual Agent Agent { get; set; } = null!;

    public virtual ICollection<AgentSkillExecution> Executions { get; set; } = new List<AgentSkillExecution>();
}
