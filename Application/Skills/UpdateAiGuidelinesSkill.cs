// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_ai_guidelines")]
public class UpdateAiGuidelinesSkill : BaseSkillImplementation
{
    private readonly IAiGuidelinesRepository _aiGuidelinesRepository;

    public UpdateAiGuidelinesSkill(IAiGuidelinesRepository aiGuidelinesRepository)
    {
        _aiGuidelinesRepository = aiGuidelinesRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var guidelinesContent = GetRequiredString(parameters, "guidelines");
        var name = parameters.TryGetValue("name", out var nameObj) ? nameObj?.ToString() ?? "AI Guidelines" : "AI Guidelines";

        await _aiGuidelinesRepository.DeactivateAllAsync(cancellationToken);

        var newGuidelines = new AiGuidelines
        {
            Id = Guid.NewGuid(),
            Name = name,
            Content = guidelinesContent,
            IsActive = true,
            Source = "chat",
            CreateTime = DateTime.UtcNow
        };

        await _aiGuidelinesRepository.AddAsync(newGuidelines, cancellationToken);

        return SkillResult.SuccessResult(
            new { newGuidelines.Name, ContentLength = guidelinesContent.Length },
            $"AI guidelines '{name}' created and activated ({guidelinesContent.Length} characters).");
    }
}
