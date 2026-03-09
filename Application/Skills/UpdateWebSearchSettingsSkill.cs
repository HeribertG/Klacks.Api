// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill zum Aktualisieren der Web-Search-Einstellungen.
/// </summary>
/// <param name="provider">Web-Search-Provider (z.B. Brave, Google)</param>
/// <param name="apiKey">API-Key für den Web-Search-Provider</param>
/// <param name="maxResults">Maximale Anzahl an Suchergebnissen</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class UpdateWebSearchSettingsSkill : BaseSkill
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IUnitOfWork _unitOfWork;

    public override string Name => "update_web_search_settings";

    public override string Description =>
        "Updates web search settings. All parameters are optional - only provided values will be updated.";

    public override SkillCategory Category => SkillCategory.Crud;

    public override IReadOnlyList<string> RequiredPermissions => new[] { "CanEditSettings" };

    public override IReadOnlyList<SkillParameter> Parameters => new[]
    {
        new SkillParameter("provider", "Web search provider name e.g. Brave, Google", SkillParameterType.String, Required: false),
        new SkillParameter("apiKey", "API key for the web search provider", SkillParameterType.String, Required: false),
        new SkillParameter("maxResults", "Maximum number of search results to return", SkillParameterType.String, Required: false),
    };

    public UpdateWebSearchSettingsSkill(
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
            { "provider", Constants.Settings.WEB_SEARCH_PROVIDER },
            { "apiKey", Constants.Settings.WEB_SEARCH_API_KEY },
            { "maxResults", Constants.Settings.WEB_SEARCH_MAX_RESULTS },
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
            return SkillResult.Error("No web search settings parameters provided to update.");

        await _unitOfWork.CompleteAsync();

        return SkillResult.SuccessResult(
            new { UpdatedFields = updatedFields },
            $"Web search settings updated ({updatedFields.Count} fields): {string.Join(", ", updatedFields)}");
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
