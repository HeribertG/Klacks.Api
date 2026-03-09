// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill zum Aktualisieren der Spam-Filter-Einstellungen.
/// </summary>
/// <param name="spamThreshold">Schwellenwert für Spam-Klassifizierung (0.0-1.0)</param>
/// <param name="uncertainThreshold">Schwellenwert für unsichere Klassifizierung (0.0-1.0)</param>
/// <param name="llmEnabled">LLM-basierte Spam-Filterung aktivieren (true/false)</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class UpdateSpamFilterSettingsSkill : BaseSkill
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IUnitOfWork _unitOfWork;

    public override string Name => "update_spam_filter_settings";

    public override string Description =>
        "Updates spam filter settings. All parameters are optional - only provided values will be updated.";

    public override SkillCategory Category => SkillCategory.Crud;

    public override IReadOnlyList<string> RequiredPermissions => new[] { "CanEditSettings" };

    public override IReadOnlyList<SkillParameter> Parameters => new[]
    {
        new SkillParameter("spamThreshold", "Spam score threshold 0.0-1.0 above which messages are classified as spam", SkillParameterType.String, Required: false),
        new SkillParameter("uncertainThreshold", "Score threshold 0.0-1.0 for uncertain/review classification", SkillParameterType.String, Required: false),
        new SkillParameter("llmEnabled", "Enable LLM-based spam filtering true/false", SkillParameterType.String, Required: false),
    };

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
        var fieldMap = new Dictionary<string, string>
        {
            { "spamThreshold", Constants.Settings.SPAM_FILTER_SPAM_THRESHOLD },
            { "uncertainThreshold", Constants.Settings.SPAM_FILTER_UNCERTAIN_THRESHOLD },
            { "llmEnabled", Constants.Settings.SPAM_FILTER_LLM_ENABLED },
        };

        var updatedFields = new List<string>();

        foreach (var (paramName, settingKey) in fieldMap)
        {
            var value = GetParameter<string>(parameters, paramName);
            if (value == null) continue;

            await UpsertSetting(settingKey, value);
            updatedFields.Add(paramName);
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
