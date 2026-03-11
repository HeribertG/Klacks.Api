// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Retrieves the AI guidelines stored as a GlobalAgentRule named "AI_GUIDELINES".
/// </summary>
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_ai_guidelines")]
public class GetAiGuidelinesSkill : BaseSkillImplementation
{
    private readonly IGlobalAgentRuleRepository _globalAgentRuleRepository;

    public GetAiGuidelinesSkill(IGlobalAgentRuleRepository globalAgentRuleRepository)
    {
        _globalAgentRuleRepository = globalAgentRuleRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var rule = await _globalAgentRuleRepository.GetRuleAsync(
            GlobalAgentRuleNames.AiGuidelines, cancellationToken);

        if (rule == null || !rule.IsActive)
        {
            return SkillResult.SuccessResult(
                new { IsConfigured = false },
                "No AI guidelines are configured yet.");
        }

        return SkillResult.SuccessResult(
            new
            {
                rule.Name,
                rule.Content,
                rule.IsActive,
                rule.Source
            },
            $"AI guidelines retrieved ({rule.Content.Length} characters).");
    }
}
