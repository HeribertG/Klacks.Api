// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class UpdateAiSoulSkill : BaseSkill
{
    private readonly IAgentSoulRepository _agentSoulRepository;
    private readonly IAgentRepository _agentRepository;

    public override string Name => "update_ai_soul";

    public override string Description =>
        "Updates a specific section of the AI assistant's personality definition (soul). " +
        "Sections: identity, personality, tone, boundaries, communication_style, values, domain_expertise, error_handling. " +
        "Each section can be independently updated. Changes are versioned with full audit trail.";

    public override SkillCategory Category => SkillCategory.Crud;

    public override IReadOnlyList<string> RequiredPermissions => new[] { "CanEditSettings" };

    public override IReadOnlyList<SkillParameter> Parameters => new[]
    {
        new SkillParameter(
            "sectionType",
            "The section type to update: identity, personality, tone, boundaries, communication_style, values, domain_expertise, error_handling.",
            SkillParameterType.String,
            Required: true),
        new SkillParameter(
            "content",
            "The content for this soul section.",
            SkillParameterType.String,
            Required: true),
        new SkillParameter(
            "sortOrder",
            "Display order (0 = first). Default: auto-assigned based on section type.",
            SkillParameterType.Integer,
            Required: false,
            DefaultValue: 0)
    };

    public UpdateAiSoulSkill(IAgentSoulRepository agentSoulRepository, IAgentRepository agentRepository)
    {
        _agentSoulRepository = agentSoulRepository;
        _agentRepository = agentRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var sectionType = GetRequiredString(parameters, "sectionType");
        var content = GetRequiredString(parameters, "content");
        var sortOrder = GetParameter<int?>(parameters, "sortOrder") ?? SoulSectionTypes.GetDefaultSortOrder(sectionType);

        var agent = await _agentRepository.GetDefaultAgentAsync(cancellationToken);
        if (agent == null)
        {
            return SkillResult.Error("No agent is configured yet.");
        }

        var section = await _agentSoulRepository.UpsertSectionAsync(
            agent.Id, sectionType, content, sortOrder,
            source: MemorySources.Chat,
            changedBy: context.UserId.ToString(),
            cancellationToken: cancellationToken);

        return SkillResult.SuccessResult(
            new { section.SectionType, ContentLength = content.Length, section.Version },
            $"Soul section '{sectionType}' updated (v{section.Version}, {content.Length} chars).");
    }
}
