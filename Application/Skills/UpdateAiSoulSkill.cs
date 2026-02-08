using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Skills;
using Klacks.Api.Domain.Services.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class UpdateAiSoulSkill : BaseSkill
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IUnitOfWork _unitOfWork;

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
            Required: true)
    };

    public UpdateAiSoulSkill(
        ISettingsRepository settingsRepository,
        IUnitOfWork unitOfWork)
    {
        _settingsRepository = settingsRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var soul = GetRequiredString(parameters, "soul");

        var existingSetting = await _settingsRepository.GetSetting(Constants.Settings.AI_SOUL);

        if (existingSetting != null)
        {
            existingSetting.Value = soul;
            await _settingsRepository.PutSetting(existingSetting);
            await _unitOfWork.CompleteAsync();

            return SkillResult.SuccessResult(
                new { Soul = soul },
                $"AI soul updated ({soul.Length} characters).");
        }

        var newSetting = new Domain.Models.Settings.Settings
        {
            Id = Guid.NewGuid(),
            Type = Constants.Settings.AI_SOUL,
            Value = soul
        };

        await _settingsRepository.AddSetting(newSetting);
        await _unitOfWork.CompleteAsync();

        return SkillResult.SuccessResult(
            new { Soul = soul },
            $"AI soul created ({soul.Length} characters).");
    }
}
