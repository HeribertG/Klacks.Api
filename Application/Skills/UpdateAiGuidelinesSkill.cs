// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Updates the AI guidelines by upserting a GlobalAgentRule named "AI_GUIDELINES".
/// </summary>
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_ai_guidelines")]
public class UpdateAiGuidelinesSkill : BaseSkillImplementation
{
    private readonly IGlobalAgentRuleRepository _globalAgentRuleRepository;

    public UpdateAiGuidelinesSkill(IGlobalAgentRuleRepository globalAgentRuleRepository)
    {
        _globalAgentRuleRepository = globalAgentRuleRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var guidelinesContent = GetRequiredString(parameters, "guidelines");

        var rule = await _globalAgentRuleRepository.UpsertRuleAsync(
            GlobalAgentRuleNames.AiGuidelines,
            guidelinesContent,
            sortOrder: 0,
            source: "chat",
            changedBy: context.UserId.ToString(),
            cancellationToken: cancellationToken);

        return SkillResult.SuccessResult(
            new { rule.Name, ContentLength = guidelinesContent.Length },
            $"AI guidelines updated ({guidelinesContent.Length} characters).");
    }
}
