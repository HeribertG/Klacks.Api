// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves the learning thresholds from the settings store, falling back to SkillLearningDefaults for
/// every key that is missing or unparsable. The thresholds live in settings rather than in code because
/// the loop starts with no traffic at all: the first real usage will show whether 3 repetitions is too
/// eager or too shy, and that must be adjustable without a deploy. The learning mode is read by name; a
/// missing or unknown value is Collect. The reference model has no code-level fallback - it is always
/// either what an installation explicitly configured (setting) or what an installation explicitly
/// configured as its default model (llm_models.is_default); a hardcoded model name would silently pick a
/// model an installation never chose.
/// </summary>
/// <param name="settingsRepository">Read access to the plain settings table</param>
/// <param name="llmRepository">Resolves the database's default model when no reference-model setting is configured</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using SettingsKeys = Klacks.Api.Application.Constants.Settings;

namespace Klacks.Api.Application.Services.Assistant.Learning;

public class SkillLearningOptionsProvider : ISkillLearningOptionsProvider
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly ILLMRepository _llmRepository;

    public SkillLearningOptionsProvider(ISettingsRepository settingsRepository, ILLMRepository llmRepository)
    {
        _settingsRepository = settingsRepository;
        _llmRepository = llmRepository;
    }

    public async Task<SkillLearningOptions> GetAsync(CancellationToken cancellationToken = default)
    {
        return new SkillLearningOptions(
            await ReadPositiveIntAsync(SettingsKeys.KLACKSY_LEARNING_MIN_OCCURRENCES, SkillLearningDefaults.MinOccurrences),
            await ReadPositiveIntAsync(SettingsKeys.KLACKSY_LEARNING_MIN_USERS, SkillLearningDefaults.MinDistinctUsers),
            await ReadPositiveIntAsync(SettingsKeys.KLACKSY_LEARNING_PRUNE_DAYS, SkillLearningDefaults.PruneDays),
            await ReadPositiveIntAsync(SettingsKeys.KLACKSY_LEARNING_RETENTION_DAYS, SkillLearningDefaults.RetentionDays),
            await ReadPositiveIntAsync(SettingsKeys.KLACKSY_LEARNING_MIN_GOLDEN_CASES, SkillLearningDefaults.MinGoldenCasesForAutoApply),
            await ReadModeAsync(),
            await ReadPositiveIntAsync(SettingsKeys.KLACKSY_LEARNING_GATE_MIN_NET_GAIN, SkillLearningDefaults.GateMinNetGain),
            await ReadReferenceModelAsync());
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

    // By name only: Enum.TryParse would also accept "1" or "2", and a number typed into the settings table
    // must not silently switch an installation to live rewriting.
    private async Task<SkillLearningMode> ReadModeAsync()
    {
        var value = (await _settingsRepository.GetSettingNoTracking(SettingsKeys.KLACKSY_LEARNING_MODE))?.Value?.Trim();
        if (string.IsNullOrEmpty(value))
        {
            return SkillLearningDefaults.Mode;
        }

        foreach (var mode in Enum.GetValues<SkillLearningMode>())
        {
            if (string.Equals(mode.ToString(), value, StringComparison.OrdinalIgnoreCase))
            {
                return mode;
            }
        }

        return SkillLearningDefaults.Mode;
    }

    // Setting first, then the database's own default model, then null - never a hardcoded model name.
    private async Task<string?> ReadReferenceModelAsync()
    {
        var value = (await _settingsRepository.GetSettingNoTracking(SettingsKeys.KLACKSY_LEARNING_REFERENCE_MODEL))?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var defaultModel = await _llmRepository.GetDefaultModelAsync();
        return defaultModel?.ModelId;
    }
}
