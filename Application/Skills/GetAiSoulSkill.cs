using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class GetAiSoulSkill : BaseSkill
{
    private readonly IAgentSoulRepository _agentSoulRepository;
    private readonly IAgentRepository _agentRepository;

    public override string Name => "get_ai_soul";

    public override string Description =>
        "Retrieves the AI assistant's personality definition (soul sections). " +
        "The soul is organized in sections: identity, personality, tone, boundaries, communication_style, values, etc.";

    public override SkillCategory Category => SkillCategory.Query;

    public override IReadOnlyList<string> RequiredPermissions => new[] { "CanViewSettings" };

    public override IReadOnlyList<SkillParameter> Parameters => new[]
    {
        new SkillParameter(
            "sectionType",
            "Optional: Filter by section type (identity, personality, tone, boundaries, communication_style, values, domain_expertise, error_handling).",
            SkillParameterType.String,
            Required: false)
    };

    public GetAiSoulSkill(IAgentSoulRepository agentSoulRepository, IAgentRepository agentRepository)
    {
        _agentSoulRepository = agentSoulRepository;
        _agentRepository = agentRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var agent = await _agentRepository.GetDefaultAgentAsync(cancellationToken);
        if (agent == null)
        {
            return SkillResult.SuccessResult(
                new { IsConfigured = false },
                "No agent is configured yet.");
        }

        var sectionType = GetParameter<string>(parameters, "sectionType");

        if (!string.IsNullOrWhiteSpace(sectionType))
        {
            var section = await _agentSoulRepository.GetSectionAsync(agent.Id, sectionType, cancellationToken);
            if (section == null)
            {
                return SkillResult.SuccessResult(
                    new { IsConfigured = false, SectionType = sectionType },
                    $"No soul section '{sectionType}' is configured.");
            }

            return SkillResult.SuccessResult(
                new { section.SectionType, section.Content, section.IsActive, section.Version, section.Source },
                $"Soul section '{sectionType}' retrieved (v{section.Version}, {section.Content.Length} chars).");
        }

        var sections = await _agentSoulRepository.GetActiveSectionsAsync(agent.Id, cancellationToken);

        if (sections.Count == 0)
        {
            return SkillResult.SuccessResult(
                new { IsConfigured = false },
                "No AI soul sections are configured yet.");
        }

        var result = sections.Select(s => new
        {
            s.SectionType,
            s.Content,
            s.SortOrder,
            s.Version,
            s.Source
        }).ToList();

        return SkillResult.SuccessResult(
            new { Sections = result, Count = result.Count },
            $"Retrieved {result.Count} soul sections.");
    }
}
