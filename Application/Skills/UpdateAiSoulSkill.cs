using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.AI;
using Klacks.Api.Domain.Models.AI;
using Klacks.Api.Domain.Models.Skills;
using Klacks.Api.Domain.Services.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class UpdateAiSoulSkill : BaseSkill
{
    private readonly IAiSoulRepository _aiSoulRepository;

    public override string Name => "update_ai_soul";

    public override string Description =>
        "Updates the AI assistant's personality definition (soul). " +
        "The soul defines the assistant's identity, values, boundaries, and communication style. " +
        "This affects how the assistant behaves and responds in all conversations.";

    public override SkillCategory Category => SkillCategory.Crud;

    public override IReadOnlyList<string> RequiredPermissions => new[] { "CanEditSettings" };

    public override IReadOnlyList<SkillParameter> Parameters => new[]
    {
        new SkillParameter(
            "soul",
            "The complete soul text defining the AI's personality, values, boundaries, and communication style.",
            SkillParameterType.String,
            Required: true),
        new SkillParameter(
            "name",
            "A short name for this soul definition (e.g. 'Klacks Planungsassistent').",
            SkillParameterType.String,
            Required: false)
    };

    public UpdateAiSoulSkill(IAiSoulRepository aiSoulRepository)
    {
        _aiSoulRepository = aiSoulRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var soulContent = GetRequiredString(parameters, "soul");
        var name = parameters.TryGetValue("name", out var nameObj) ? nameObj?.ToString() ?? "AI Soul" : "AI Soul";

        await _aiSoulRepository.DeactivateAllAsync(cancellationToken);

        var newSoul = new AiSoul
        {
            Id = Guid.NewGuid(),
            Name = name,
            Content = soulContent,
            IsActive = true,
            Source = "chat",
            CreateTime = DateTime.UtcNow
        };

        await _aiSoulRepository.AddAsync(newSoul, cancellationToken);

        return SkillResult.SuccessResult(
            new { newSoul.Name, ContentLength = soulContent.Length },
            $"AI soul '{name}' created and activated ({soulContent.Length} characters).");
    }
}
