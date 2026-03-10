// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill for updating spam filter settings (thresholds and LLM classification toggle).
/// </summary>
/// <param name="spamThreshold">Spam classification threshold (0.0–1.0)</param>
/// <param name="uncertainThreshold">Uncertain classification threshold (0.0–1.0)</param>
/// <param name="llmEnabled">Enable or disable LLM-based spam filtering</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_spam_filter_settings")]
public class UpdateSpamFilterSettingsSkill : BaseSkillImplementation
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSpamFilterSettingsSkill(
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
        var updatedFields = new List<string>();

        var spamThreshold = GetParameter<decimal?>(parameters, "spamThreshold");
        if (spamThreshold.HasValue)
        {
            await UpsertSetting(Settings.SPAM_FILTER_SPAM_THRESHOLD, spamThreshold.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            updatedFields.Add("spamThreshold");
        }

        var uncertainThreshold = GetParameter<decimal?>(parameters, "uncertainThreshold");
        if (uncertainThreshold.HasValue)
        {
            await UpsertSetting(Settings.SPAM_FILTER_UNCERTAIN_THRESHOLD, uncertainThreshold.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            updatedFields.Add("uncertainThreshold");
        }

        var llmEnabled = GetParameter<bool?>(parameters, "llmEnabled");
        if (llmEnabled.HasValue)
        {
            await UpsertSetting(Settings.SPAM_FILTER_LLM_ENABLED, llmEnabled.Value.ToString().ToLowerInvariant());
            updatedFields.Add("llmEnabled");
        }

        if (updatedFields.Count == 0)
            return SkillResult.Error("No spam filter settings parameters provided to update.");

        await _unitOfWork.CompleteAsync();

        return SkillResult.SuccessResult(
            new { UpdatedFields = updatedFields },
            $"Spam filter settings updated ({updatedFields.Count} fields): {string.Join(", ", updatedFields)}");
    }

    private async Task UpsertSetting(string settingType, string value)
    {
        var existing = await _settingsRepository.GetSetting(settingType);
        if (existing != null)
        {
            existing.Value = value;
            await _settingsRepository.PutSetting(existing);
        }
        else
        {
            var newSetting = new Domain.Models.Settings.Settings
            {
                Id = Guid.NewGuid(),
                Type = settingType,
                Value = value
            };
            await _settingsRepository.AddSetting(newSetting);
        }
    }
}
