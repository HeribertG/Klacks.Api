// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Meta-skill that updates an existing skill's description, enabled status, or handler steps,
/// then reloads the skill registry so changes take effect immediately.
/// </summary>
/// <param name="skillName">Name of the skill to update (required)</param>
/// <param name="description">New LLM-facing description (optional)</param>
/// <param name="isEnabled">Enable or disable the skill (optional)</param>
/// <param name="handlerSteps">New handler steps JSON array (optional)</param>

using System.Text.Json;
using Klacks.Api.Application.Services.Assistant;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills.Meta;

[SkillImplementation("update_agent_skill")]
public class UpdateAgentSkillSkill : BaseSkillImplementation
{
    private readonly IAgentSkillRepository _agentSkillRepository;
    private readonly SkillRegistryInitializer _skillRegistryInitializer;

    public UpdateAgentSkillSkill(
        IAgentSkillRepository agentSkillRepository,
        SkillRegistryInitializer skillRegistryInitializer)
    {
        _agentSkillRepository = agentSkillRepository;
        _skillRegistryInitializer = skillRegistryInitializer;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var skillName = GetRequiredString(parameters, "skillName");
        var description = GetParameter<string>(parameters, "description");
        var isEnabled = GetParameter<bool?>(parameters, "isEnabled");
        var handlerSteps = GetParameter<string>(parameters, "handlerSteps");

        var allSkills = await _agentSkillRepository.GetAllEnabledAsync(cancellationToken);
        var skill = allSkills.FirstOrDefault(s => s.Name.Equals(skillName, StringComparison.OrdinalIgnoreCase));

        if (skill == null)
        {
            return SkillResult.Error($"Skill '{skillName}' not found.");
        }

        var updated = false;

        if (!string.IsNullOrWhiteSpace(description))
        {
            skill.Description = description;
            updated = true;
        }

        if (isEnabled.HasValue)
        {
            skill.IsEnabled = isEnabled.Value;
            updated = true;
        }

        if (!string.IsNullOrWhiteSpace(handlerSteps))
        {
            skill.HandlerConfig = BuildHandlerConfig(handlerSteps);
            updated = true;
        }

        if (!updated)
        {
            return SkillResult.Error("No fields to update were provided.");
        }

        skill.Version += 1;

        await _agentSkillRepository.UpdateAsync(skill, cancellationToken);
        await _skillRegistryInitializer.InitializeAsync(cancellationToken);

        return SkillResult.SuccessResult(
            new { SkillName = skill.Name, Version = skill.Version },
            $"Skill '{skill.Name}' updated to version {skill.Version}.");
    }

    private static string BuildHandlerConfig(string handlerSteps)
    {
        try
        {
            using var doc = JsonDocument.Parse(handlerSteps);
            return JsonSerializer.Serialize(new { steps = doc.RootElement });
        }
        catch
        {
            return $"{{\"steps\":{handlerSteps}}}";
        }
    }
}
