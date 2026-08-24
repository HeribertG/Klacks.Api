// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Definition of a single skill for seeding into the AgentSkill database table.
/// </summary>
/// <param name="Name">Unique skill identifier (snake_case)</param>
/// <param name="Description">LLM-facing description that determines when the skill is selected</param>
/// <param name="Category">Skill category (Query, Crud, Action, UI, System, Validation)</param>
/// <param name="ExecutionType">How the skill is executed (Skill, UiAction, FrontendOnly, UiPassthrough)</param>
/// <param name="PairedApplySkill">
/// Optional internal name of the skill that PERSISTS what this skill only proposed. Set it on a
/// read-only propose_* / preview_* skill to name its apply_* counterpart. Declaring the pair here
/// rather than deriving it from the name keeps it valid when skills are merged or renamed, and lets
/// the toolset assembler offer the apply skill again on a bare confirmation turn ("ja", "oui").
/// It only affects which tools are offered — the apply call still passes the autonomy gate.
/// </param>
/// <param name="Effect">
/// Skill effect taxonomy value (Explain, Read, Advise or Mutate) as a string, parsed case-insensitively.
/// Missing or unparsable values fall back to AgentSkillDefaults.Effect (fail-closed: Mutate).
/// </param>
namespace Klacks.Api.Infrastructure.Persistence.Seed.Models;

public class SkillSeedDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "Query";
    public string ExecutionType { get; set; } = "Skill";
    public List<string> RequiredPermissions { get; set; } = new();
    public List<SkillSeedParameter>? Parameters { get; set; }
    public object? HandlerConfig { get; set; }
    public string? HandlerType { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool AlwaysOn { get; set; }
    public string? PairedApplySkill { get; set; }
    public string? Effect { get; set; }
    [System.Text.Json.Serialization.JsonConverter(typeof(TriggerKeywordGroupsConverter))]
    public Dictionary<string, List<string>>? TriggerKeywords { get; set; }
    public Dictionary<string, List<string>>? Synonyms { get; set; }
    public int Version { get; set; } = 1;
}
