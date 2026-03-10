// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Meta-skill that lists all registered skills from the database for self-reflection.
/// Supports optional filtering by searchTerm and executionType.
/// </summary>

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills.Meta;

[SkillImplementation("list_agent_skills")]
public class ListAgentSkillsSkill : BaseSkillImplementation
{
    private readonly IAgentSkillRepository _agentSkillRepository;

    public ListAgentSkillsSkill(IAgentSkillRepository agentSkillRepository)
    {
        _agentSkillRepository = agentSkillRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var searchTerm = GetParameter<string>(parameters, "searchTerm");
        var executionType = GetParameter<string>(parameters, "executionType");

        var allSkills = await _agentSkillRepository.GetAllEnabledAsync(cancellationToken);

        var filtered = allSkills.AsEnumerable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            filtered = filtered.Where(s =>
                s.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                s.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(executionType))
        {
            filtered = filtered.Where(s =>
                s.ExecutionType.Equals(executionType, StringComparison.OrdinalIgnoreCase));
        }

        var skills = filtered
            .Select(s => new
            {
                s.Name,
                s.Description,
                s.ExecutionType,
                s.Category,
                s.IsEnabled
            })
            .ToList();

        var resultData = new
        {
            Skills = skills,
            TotalCount = skills.Count
        };

        var message = $"Found {skills.Count} skill(s)" +
                      (!string.IsNullOrEmpty(searchTerm) ? $" matching '{searchTerm}'" : "") +
                      (!string.IsNullOrEmpty(executionType) ? $" of type '{executionType}'" : "") + ".";

        return SkillResult.SuccessResult(resultData, message);
    }
}
