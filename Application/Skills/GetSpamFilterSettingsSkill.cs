// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill zum Lesen der Spam-Filter-Konfiguration.
/// </summary>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class GetSpamFilterSettingsSkill : BaseSkill
{
    private readonly ISettingsRepository _settingsRepository;

    public override string Name => "get_spam_filter_settings";

    public override string Description =>
        "Retrieves spam filter configuration including spam threshold, uncertain threshold, and whether LLM-based filtering is enabled.";

    public override SkillCategory Category => SkillCategory.Query;

    public override IReadOnlyList<string> RequiredPermissions => new[] { "CanViewSettings" };

    public override IReadOnlyList<SkillParameter> Parameters => Array.Empty<SkillParameter>();

    public GetSpamFilterSettingsSkill(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var spamThreshold = await GetSettingValue(Constants.Settings.SPAM_FILTER_SPAM_THRESHOLD);
        var uncertainThreshold = await GetSettingValue(Constants.Settings.SPAM_FILTER_UNCERTAIN_THRESHOLD);
        var llmEnabled = await GetSettingValue(Constants.Settings.SPAM_FILTER_LLM_ENABLED);

        var resultData = new
        {
            SpamThreshold = spamThreshold,
            UncertainThreshold = uncertainThreshold,
            LlmEnabled = llmEnabled
        };

        return SkillResult.SuccessResult(resultData,
            $"Spam filter settings: SpamThreshold={spamThreshold}, UncertainThreshold={uncertainThreshold}, LlmEnabled={llmEnabled}.");
    }

    private async Task<string> GetSettingValue(string settingType)
    {
        var setting = await _settingsRepository.GetSetting(settingType);
        return setting?.Value ?? "";
    }
}
