// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_ai_soul")]
public class UpdateAiSoulSkill : BaseSkillImplementation
{
    private readonly IAgentSoulRepository _agentSoulRepository;
    private readonly IAgentRepository _agentRepository;

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
