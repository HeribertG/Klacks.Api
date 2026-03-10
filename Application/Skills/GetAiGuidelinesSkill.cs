// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_ai_guidelines")]
public class GetAiGuidelinesSkill : BaseSkillImplementation
{
    private readonly IAiGuidelinesRepository _aiGuidelinesRepository;

    public GetAiGuidelinesSkill(IAiGuidelinesRepository aiGuidelinesRepository)
    {
        _aiGuidelinesRepository = aiGuidelinesRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var activeGuidelines = await _aiGuidelinesRepository.GetActiveAsync(cancellationToken);

        if (activeGuidelines == null)
        {
            return SkillResult.SuccessResult(
                new { IsConfigured = false },
                "No AI guidelines are configured yet.");
        }

        return SkillResult.SuccessResult(
            new
            {
                activeGuidelines.Name,
                activeGuidelines.Content,
                activeGuidelines.IsActive,
                activeGuidelines.Source
            },
            $"AI guidelines '{activeGuidelines.Name}' retrieved ({activeGuidelines.Content.Length} characters).");
    }
}
