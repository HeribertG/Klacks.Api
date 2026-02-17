using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class UpdateAiGuidelinesSkill : BaseSkill
{
    private readonly IAiGuidelinesRepository _aiGuidelinesRepository;

    public override string Name => "update_ai_guidelines";

    public override string Description =>
        "Updates the AI assistant's guidelines. " +
        "Guidelines define rules for behavior, function usage, and permission handling. " +
        "This affects how the assistant operates in all conversations.";

    public override SkillCategory Category => SkillCategory.Crud;

    public override IReadOnlyList<string> RequiredPermissions => new[] { "CanEditSettings" };

    public override IReadOnlyList<SkillParameter> Parameters => new[]
    {
        new SkillParameter(
            "guidelines",
            "The complete guidelines text defining the AI's behavioral rules.",
            SkillParameterType.String,
            Required: true),
        new SkillParameter(
            "name",
            "A short name for this guidelines set (e.g. 'Default Guidelines').",
            SkillParameterType.String,
            Required: false)
    };

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
