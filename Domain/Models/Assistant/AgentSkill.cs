// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;

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

    public SkillEffect Effect { get; set; } = AgentSkillDefaults.Effect;

    public string HandlerConfig { get; set; } = "{}";

    public string TriggerKeywords { get; set; } = "{}";

    public string AllowedChannels { get; set; } = "[]";

    public bool AlwaysOn { get; set; }

    public string? PairedApplySkill { get; set; }

    public int Version { get; set; } = 1;

    public Dictionary<string, List<string>>? Synonyms { get; set; }

    /// <summary>
    /// Short, user-facing name of this skill per language tag, authored by hand (spec §1 rule 4). The
    /// counterpart of Synonyms and its exact storage twin - a jsonb dictionary - but the opposite
    /// direction: Synonyms are INPUT vocabulary that decides whether this skill is selected, Labels are
    /// OUTPUT text that names it back to the user, and they must therefore never reach the matching or
    /// retrieval path. Core languages come from skill-seeds.json, the other 21 from each language pack's
    /// skill-labels.json. Null or a missing language means the assistant names this skill in no question
    /// at all; it never substitutes English.
    /// </summary>
    public Dictionary<string, string>? Labels { get; set; }

    public virtual Agent Agent { get; set; } = null!;

    public virtual ICollection<AgentSkillExecution> Executions { get; set; } = new List<AgentSkillExecution>();
}
