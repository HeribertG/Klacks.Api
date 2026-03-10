// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Base class for settings reader skills. Reads settings keys from the repository
/// and builds a result dictionary. Handles sensitive fields (HasApiKey pattern).
/// </summary>
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills.Base;

public abstract class SettingsReaderSkillBase : BaseSkill
{
    private readonly ISettingsRepository _settingsRepository;

    protected abstract SettingsSkillConfig SkillConfig { get; }

    public override string Name => SkillConfig.SkillName;
    public override string Description => SkillConfig.SkillDescription;
    public override SkillCategory Category => SkillConfig.SkillCategory;
    public override IReadOnlyList<string> RequiredPermissions => SkillConfig.Permissions;
    public override IReadOnlyList<SkillParameter> Parameters => Array.Empty<SkillParameter>();

    protected SettingsReaderSkillBase(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, object>();

        foreach (var field in SkillConfig.Fields)
        {
            var setting = await _settingsRepository.GetSetting(field.SettingsKey);
            result[field.ResultName] = setting?.Value ?? "";
        }

        if (SkillConfig.SensitiveFields is not null)
        {
            foreach (var field in SkillConfig.SensitiveFields)
            {
                var setting = await _settingsRepository.GetSetting(field.SettingsKey);
                result[field.ResultName] = !string.IsNullOrWhiteSpace(setting?.Value);
            }
        }

        return SkillResult.SuccessResult(result, SkillConfig.ResultMessage);
    }
}
