// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves the learning thresholds from the settings store, falling back to SkillLearningDefaults for
/// every key that is missing or unparsable. The thresholds live in settings rather than in code because
/// the loop starts with no traffic at all: the first real usage will show whether 3 repetitions is too
/// eager or too shy, and that must be adjustable without a deploy.
/// </summary>
/// <param name="settingsRepository">Read access to the plain settings table</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using SettingsKeys = Klacks.Api.Application.Constants.Settings;

namespace Klacks.Api.Application.Services.Assistant.Learning;

public class SkillLearningOptionsProvider : ISkillLearningOptionsProvider
{
    private readonly ISettingsRepository _settingsRepository;

    public SkillLearningOptionsProvider(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public async Task<SkillLearningOptions> GetAsync(CancellationToken cancellationToken = default)
    {
        return new SkillLearningOptions(
            await ReadPositiveIntAsync(SettingsKeys.KLACKSY_LEARNING_MIN_OCCURRENCES, SkillLearningDefaults.MinOccurrences),
            await ReadPositiveIntAsync(SettingsKeys.KLACKSY_LEARNING_MIN_USERS, SkillLearningDefaults.MinDistinctUsers),
            await ReadPositiveIntAsync(SettingsKeys.KLACKSY_LEARNING_PRUNE_DAYS, SkillLearningDefaults.PruneDays),
            await ReadPositiveIntAsync(SettingsKeys.KLACKSY_LEARNING_RETENTION_DAYS, SkillLearningDefaults.RetentionDays));
    }

    private async Task<int> ReadPositiveIntAsync(string key, int fallback)
    {
        var setting = await _settingsRepository.GetSettingNoTracking(key);
        if (setting == null || !int.TryParse(setting.Value, out var parsed) || parsed <= 0)
        {
            return fallback;
        }

        return parsed;
    }
}
