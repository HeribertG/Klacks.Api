// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Definition of a single skill for seeding into the AgentSkill database table.
/// </summary>
/// <param name="Name">Unique skill identifier (snake_case)</param>
/// <param name="Description">LLM-facing description that determines when the skill is selected</param>
/// <param name="Category">Skill category (Query, Crud, Action, UI, System, Validation)</param>
/// <param name="ExecutionType">How the skill is executed (Skill, UiAction, FrontendOnly, UiPassthrough)</param>
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
    public int? MaxCallsPerSession { get; set; }
    public List<string>? TriggerKeywords { get; set; }
    public Dictionary<string, List<string>>? Synonyms { get; set; }
    public int Version { get; set; } = 1;
}
