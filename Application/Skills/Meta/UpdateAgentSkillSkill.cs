// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Meta-skill that updates an existing skill's description, enabled status, handler steps,
/// trigger keywords, or synonyms, then reloads the skill registry so changes take effect immediately.
/// </summary>
/// <param name="skillName">Name of the skill to update (required)</param>
/// <param name="description">New LLM-facing description (optional)</param>
/// <param name="isEnabled">Enable or disable the skill (optional)</param>
/// <param name="handlerSteps">New handler steps JSON array (optional)</param>
/// <param name="triggerKeywords">New comma-separated trigger keywords (optional)</param>
/// <param name="synonyms">JSON object mapping language codes to synonym lists, e.g. {"de":["wort1"],"en":["word1"]} (optional)</param>

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
        var triggerKeywords = GetParameter<string>(parameters, "triggerKeywords");
        var synonymsJson = GetParameter<string>(parameters, "synonyms");

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

        if (!string.IsNullOrWhiteSpace(triggerKeywords))
        {
            var keywords = triggerKeywords
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(k => $"\"{k}\"");
            skill.TriggerKeywords = $"[{string.Join(",", keywords)}]";
            updated = true;
        }

        if (!string.IsNullOrWhiteSpace(synonymsJson))
        {
            var parseResult = ParseSynonyms(synonymsJson);
            if (parseResult.Error != null)
            {
                return SkillResult.Error(parseResult.Error);
            }
            skill.Synonyms = parseResult.Value;
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

    private static (Dictionary<string, List<string>>? Value, string? Error) ParseSynonyms(string json)
    {
        try
        {
            var result = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json);
            if (result == null)
            {
                return (null, "Synonyms JSON parsed to null. Expected an object like {\"de\":[\"wort1\"],\"en\":[\"word1\"]}.");
            }
            return (result, null);
        }
        catch (JsonException ex)
        {
            return (null, $"Invalid synonyms JSON: {ex.Message}");
        }
    }
}
