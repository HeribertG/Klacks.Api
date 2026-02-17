using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class GetAiSoulSkill : BaseSkill
{
    private readonly IAiSoulRepository _aiSoulRepository;

    public override string Name => "get_ai_soul";

    public override string Description =>
        "Retrieves the AI assistant's personality definition (soul). " +
        "The soul defines the assistant's identity, values, boundaries, and communication style.";

    public override SkillCategory Category => SkillCategory.Query;

    public override IReadOnlyList<string> RequiredPermissions => new[] { "CanViewSettings" };

    public override IReadOnlyList<SkillParameter> Parameters => Array.Empty<SkillParameter>();

    public GetAiSoulSkill(IAiSoulRepository aiSoulRepository)
    {
        _aiSoulRepository = aiSoulRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var activeSoul = await _aiSoulRepository.GetActiveAsync(cancellationToken);

        if (activeSoul == null)
        {
            return SkillResult.SuccessResult(
                new { IsConfigured = false },
                "No AI soul is configured yet.");
        }

        return SkillResult.SuccessResult(
            new
            {
                activeSoul.Name,
                activeSoul.Content,
                activeSoul.IsActive,
                activeSoul.Source
            },
            $"AI soul '{activeSoul.Name}' retrieved ({activeSoul.Content.Length} characters).");
    }
}
