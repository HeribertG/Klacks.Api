// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill zum Lesen der Web-Search-Konfiguration.
/// </summary>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class GetWebSearchSettingsSkill : BaseSkill
{
    private readonly ISettingsRepository _settingsRepository;

    public override string Name => "get_web_search_settings";

    public override string Description =>
        "Retrieves web search configuration including provider, max results, and whether an API key is configured.";

    public override SkillCategory Category => SkillCategory.Query;

    public override IReadOnlyList<string> RequiredPermissions => new[] { "CanViewSettings" };

    public override IReadOnlyList<SkillParameter> Parameters => Array.Empty<SkillParameter>();

    public GetWebSearchSettingsSkill(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var provider = await GetSettingValue(Constants.Settings.WEB_SEARCH_PROVIDER);
        var maxResults = await GetSettingValue(Constants.Settings.WEB_SEARCH_MAX_RESULTS);
        var apiKey = await GetSettingValue(Constants.Settings.WEB_SEARCH_API_KEY);
        var hasApiKey = !string.IsNullOrWhiteSpace(apiKey);

        var resultData = new
        {
            Provider = provider,
            MaxResults = maxResults,
            HasApiKey = hasApiKey
        };

        return SkillResult.SuccessResult(resultData,
            $"Web search settings: Provider={provider}, MaxResults={maxResults}, API key is {(hasApiKey ? "configured" : "not configured")}.");
    }

    private async Task<string> GetSettingValue(string settingType)
    {
        var setting = await _settingsRepository.GetSetting(settingType);
        return setting?.Value ?? "";
    }
}
