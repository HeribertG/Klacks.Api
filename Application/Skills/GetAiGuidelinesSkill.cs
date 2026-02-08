using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.AI;
using Klacks.Api.Domain.Models.Skills;
using Klacks.Api.Domain.Services.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class GetAiGuidelinesSkill : BaseSkill
{
    private readonly IAiGuidelinesRepository _aiGuidelinesRepository;

    public override string Name => "get_ai_guidelines";

    public override string Description =>
        "Retrieves the AI assistant's guidelines. " +
        "Guidelines define rules for behavior, function usage, and permission handling.";

    public override SkillCategory Category => SkillCategory.Query;

    public override IReadOnlyList<string> RequiredPermissions => new[] { "CanViewSettings" };

    public override IReadOnlyList<SkillParameter> Parameters => Array.Empty<SkillParameter>();

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
