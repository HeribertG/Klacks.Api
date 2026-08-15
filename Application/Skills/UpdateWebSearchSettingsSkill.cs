// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill for updating the web search provider configuration.
/// </summary>
/// <param name="provider">Web search provider (serper or tavily)</param>
/// <param name="apiKey">API key for the selected provider</param>
/// <param name="maxResults">Maximum number of search results per query</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_web_search_settings")]
public class UpdateWebSearchSettingsSkill : BaseSkillImplementation
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISettingsEncryptionService _encryptionService;

    public UpdateWebSearchSettingsSkill(
        ISettingsRepository settingsRepository,
        IUnitOfWork unitOfWork,
        ISettingsEncryptionService encryptionService)
    {
        _settingsRepository = settingsRepository;
        _unitOfWork = unitOfWork;
        _encryptionService = encryptionService;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var updatedFields = new List<string>();

        var provider = GetParameter<string>(parameters, "provider");
        if (provider != null)
        {
            await UpsertSetting(Settings.WEB_SEARCH_PROVIDER, provider);
            updatedFields.Add("provider");
        }

        var apiKey = GetParameter<string>(parameters, "apiKey");
        if (apiKey != null)
        {
            await UpsertSetting(Settings.WEB_SEARCH_API_KEY, apiKey);
            updatedFields.Add("apiKey");
        }

        var maxResults = GetParameter<int?>(parameters, "maxResults");
        if (maxResults.HasValue)
        {
            await UpsertSetting(Settings.WEB_SEARCH_MAX_RESULTS, maxResults.Value.ToString());
            updatedFields.Add("maxResults");
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
        var storedValue = _encryptionService.ProcessForStorage(settingType, value);

        var existing = await _settingsRepository.GetSetting(settingType);
        if (existing != null)
        {
            existing.Value = storedValue;
            await _settingsRepository.PutSetting(existing);
        }
        else
        {
            var newSetting = new Domain.Models.Settings.Settings
            {
                Id = Guid.NewGuid(),
                Type = settingType,
                Value = storedValue
            };
            await _settingsRepository.AddSetting(newSetting);
        }
    }
}
