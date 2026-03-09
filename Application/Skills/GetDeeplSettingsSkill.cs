// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill zum Lesen der DeepL-Übersetzungs-API-Einstellungen.
/// </summary>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class GetDeeplSettingsSkill : BaseSkill
{
    private readonly ISettingsRepository _settingsRepository;

    public override string Name => "get_deepl_settings";

    public override string Description =>
        "Retrieves the DeepL translation API settings. " +
        "Returns whether an API key is configured (not the key itself for security).";

    public override SkillCategory Category => SkillCategory.Query;

    public override IReadOnlyList<string> RequiredPermissions => new[] { "CanViewSettings" };

    public override IReadOnlyList<SkillParameter> Parameters => Array.Empty<SkillParameter>();

    public GetDeeplSettingsSkill(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var apiKey = await GetSettingValue("DEEPL_API_KEY");
        var hasApiKey = !string.IsNullOrWhiteSpace(apiKey);

        var resultData = new
        {
            HasApiKey = hasApiKey,
            SettingsCard = "DeepL Translation"
        };

        var status = hasApiKey ? "configured" : "not configured";
        return SkillResult.SuccessResult(resultData, $"DeepL settings: API key is {status}.");
    }

    private async Task<string> GetSettingValue(string settingType)
    {
        var setting = await _settingsRepository.GetSetting(settingType);
        return setting?.Value ?? "";
    }
}
