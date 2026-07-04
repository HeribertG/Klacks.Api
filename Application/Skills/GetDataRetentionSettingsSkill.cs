// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_data_retention_settings")]
public class GetDataRetentionSettingsSkill : BaseSkillImplementation
{
    private readonly ISettingsRepository _settingsRepository;

    public GetDataRetentionSettingsSkill(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var retentionSetting = await _settingsRepository.GetSetting(Constants.Settings.DATA_RETENTION_DAYS);
        var retentionDays = retentionSetting is not null && int.TryParse(retentionSetting.Value, out var days) && days > 0
            ? days
            : Constants.Settings.DATA_RETENTION_DAYS_DEFAULT;

        var resultData = new
        {
            RetentionDays = retentionDays,
            SettingsCard = "DataRetention",
            AvailableFields = new[]
            {
                new { Field = "retentionDays", Description = "Number of days after which soft-deleted records are permanently purged (min 30, max 36500)", CurrentValue = retentionDays.ToString() }
            }
        };

        return SkillResult.SuccessResult(resultData, $"Data retention period retrieved: {retentionDays} days.");
    }
}
