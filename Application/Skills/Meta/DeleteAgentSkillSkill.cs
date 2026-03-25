// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Meta-skill that soft-deletes a user-created skill by disabling it.
/// System skills (skills with a C# implementation) cannot be deleted.
/// </summary>
/// <param name="skillName">Name of the skill to disable (required)</param>

using Klacks.Api.Application.Services.Assistant;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills.Meta;

[SkillImplementation("delete_agent_skill")]
public class DeleteAgentSkillSkill : BaseSkillImplementation
{
    private readonly IAgentSkillRepository _agentSkillRepository;
    private readonly ISkillRegistry _skillRegistry;
    private readonly SkillRegistryInitializer _skillRegistryInitializer;
    private readonly ISkillCacheService _skillCacheService;

    public DeleteAgentSkillSkill(
        IAgentSkillRepository agentSkillRepository,
        ISkillRegistry skillRegistry,
        SkillRegistryInitializer skillRegistryInitializer,
        ISkillCacheService skillCacheService)
    {
        _agentSkillRepository = agentSkillRepository;
        _skillRegistry = skillRegistry;
        _skillRegistryInitializer = skillRegistryInitializer;
        _skillCacheService = skillCacheService;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var skillName = GetRequiredString(parameters, "skillName");

        var allSkills = await _agentSkillRepository.GetAllEnabledAsync(cancellationToken);
        var skill = allSkills.FirstOrDefault(s => s.Name.Equals(skillName, StringComparison.OrdinalIgnoreCase));

        if (skill == null)
        {
            return SkillResult.Error($"Skill '{skillName}' not found or is already disabled.");
        }

        var descriptor = _skillRegistry.GetSkillByName(skill.Name);
        var isSystemSkill = descriptor?.ImplementationType != null;

        if (isSystemSkill)
        {
            return SkillResult.Error(
                "Cannot delete system skills. Use update_agent_skill to disable instead.");
        }

        skill.IsEnabled = false;

        await _agentSkillRepository.UpdateAsync(skill, cancellationToken);
        _skillCacheService.InvalidateCache();
        await _skillRegistryInitializer.InitializeAsync(cancellationToken);

        return SkillResult.SuccessResult(
            new { SkillName = skill.Name },
            $"Skill '{skill.Name}' has been disabled and is no longer available.");
    }
}
