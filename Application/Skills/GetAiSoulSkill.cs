using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Skills;
using Klacks.Api.Domain.Services.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class GetAiSoulSkill : BaseSkill
{
    private readonly ISettingsRepository _settingsRepository;

    public override string Name => "get_ai_soul";

    public override string Description =>
        "Retrieves the AI assistant's personality definition (soul). " +
        "The soul defines the assistant's identity, values, boundaries, and communication style.";

    public override SkillCategory Category => SkillCategory.Query;

    public override IReadOnlyList<string> RequiredPermissions => new[] { "CanViewSettings" };

    public override IReadOnlyList<SkillParameter> Parameters => Array.Empty<SkillParameter>();

    public GetAiSoulSkill(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var soulSetting = await _settingsRepository.GetSetting(Constants.Settings.AI_SOUL);

        var soulText = soulSetting?.Value ?? "";

        return SkillResult.SuccessResult(
            new { Soul = soulText, IsConfigured = !string.IsNullOrWhiteSpace(soulText) },
            string.IsNullOrWhiteSpace(soulText)
                ? "No AI soul is configured yet."
                : $"AI soul retrieved ({soulText.Length} characters).");
    }
}
