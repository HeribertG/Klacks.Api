// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_general_settings")]
public class GetGeneralSettingsSkill : BaseSkillImplementation
{
    private readonly ISettingsRepository _settingsRepository;

    public GetGeneralSettingsSkill(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var appNameSetting = await _settingsRepository.GetSetting(Constants.Settings.APP_NAME);

        var resultData = new
        {
            AppName = appNameSetting?.Value ?? "",
            SettingsCard = "General",
            AvailableFields = new[]
            {
                new { Field = "appName", Description = "Application name displayed in the browser title and header", CurrentValue = appNameSetting?.Value ?? "" }
            }
        };

        return SkillResult.SuccessResult(resultData, $"General settings retrieved. App name: '{appNameSetting?.Value ?? "(not set)"}'");
    }
}
