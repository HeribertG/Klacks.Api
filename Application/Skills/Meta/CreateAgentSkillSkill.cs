// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Meta-skill that creates a new UiAction skill at runtime, persists it to the database,
/// and reloads the skill registry so the new skill is immediately available.
/// </summary>
/// <param name="name">Skill name in snake_case (required)</param>
/// <param name="description">LLM-facing description (required)</param>
/// <param name="category">Skill category (optional, default: Action)</param>
/// <param name="handlerSteps">JSON array of handler steps for UiAction (optional)</param>
/// <param name="triggerKeywords">Comma-separated keywords that trigger this skill (optional)</param>

using System.Text.Json;
using Klacks.Api.Application.Services.Assistant;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills.Meta;

[SkillImplementation("create_agent_skill")]
public class CreateAgentSkillSkill : BaseSkillImplementation
{
    private readonly IAgentSkillRepository _agentSkillRepository;
    private readonly IAgentRepository _agentRepository;
    private readonly SkillRegistryInitializer _skillRegistryInitializer;

    public CreateAgentSkillSkill(
        IAgentSkillRepository agentSkillRepository,
        IAgentRepository agentRepository,
        SkillRegistryInitializer skillRegistryInitializer)
    {
        _agentSkillRepository = agentSkillRepository;
        _agentRepository = agentRepository;
        _skillRegistryInitializer = skillRegistryInitializer;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var name = GetRequiredString(parameters, "name");
        var description = GetRequiredString(parameters, "description");
        var category = GetParameter<string>(parameters, "category") ?? "Action";
        var handlerSteps = GetParameter<string>(parameters, "handlerSteps");
        var triggerKeywords = GetParameter<string>(parameters, "triggerKeywords");

        name = name.Trim().ToLowerInvariant().Replace(' ', '_');

        if (!IsValidSnakeCase(name))
        {
            return SkillResult.Error($"Skill name '{name}' is invalid. Use snake_case with letters, digits, and underscores only.");
        }

        var existing = await _agentSkillRepository.GetAllEnabledAsync(cancellationToken);
        if (existing.Any(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            return SkillResult.Error($"A skill with the name '{name}' already exists. Use update_agent_skill to modify it.");
        }

        var agent = await _agentRepository.GetDefaultAgentAsync(cancellationToken);
        if (agent == null)
        {
            return SkillResult.Error("No default agent found. Cannot create skill.");
        }

        var handlerConfig = "{}";
        if (!string.IsNullOrWhiteSpace(handlerSteps))
        {
            handlerConfig = BuildHandlerConfig(handlerSteps);
        }

        var keywordsJson = "[]";
        if (!string.IsNullOrWhiteSpace(triggerKeywords))
        {
            var keywords = triggerKeywords
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(k => $"\"{k}\"");
            keywordsJson = $"[{string.Join(",", keywords)}]";
        }

        var agentSkill = new AgentSkill
        {
            AgentId = agent.Id,
            Name = name,
            Description = description,
            Category = category,
            ExecutionType = LlmExecutionTypes.UiAction,
            HandlerConfig = handlerConfig,
            TriggerKeywords = keywordsJson,
            IsEnabled = true,
            Version = 1
        };

        await _agentSkillRepository.AddAsync(agentSkill, cancellationToken);
        await _skillRegistryInitializer.InitializeAsync(cancellationToken);

        return SkillResult.SuccessResult(
            new { SkillName = name },
            $"Skill '{name}' created and immediately available.");
    }

    private static bool IsValidSnakeCase(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        return name.All(c => char.IsLetterOrDigit(c) || c == '_');
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
