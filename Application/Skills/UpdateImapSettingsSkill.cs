// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class UpdateImapSettingsSkill : BaseSkill
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IUnitOfWork _unitOfWork;

    public override string Name => "update_imap_settings";

    public override string Description =>
        "Updates the incoming email (IMAP) settings. " +
        "All parameters are optional - only provided values will be updated.";

    public override SkillCategory Category => SkillCategory.Crud;

    public override IReadOnlyList<string> RequiredPermissions => new[] { "CanEditSettings" };

    public override IReadOnlyList<SkillParameter> Parameters => new[]
    {
        new SkillParameter("server", "IMAP server hostname", SkillParameterType.String, Required: false),
        new SkillParameter("port", "IMAP server port (e.g. 993, 143)", SkillParameterType.String, Required: false),
        new SkillParameter("enableSSL", "Enable SSL/TLS (true or false)", SkillParameterType.String, Required: false),
        new SkillParameter("folder", "IMAP folder to monitor (e.g. INBOX)", SkillParameterType.String, Required: false),
        new SkillParameter("pollInterval", "Poll interval in seconds", SkillParameterType.String, Required: false),
        new SkillParameter("username", "IMAP authentication username", SkillParameterType.String, Required: false),
        new SkillParameter("password", "IMAP authentication password", SkillParameterType.String, Required: false),
    };

    public UpdateImapSettingsSkill(
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
            { "server", Constants.Settings.APP_INCOMING_SERVER },
            { "port", Constants.Settings.APP_INCOMING_SERVER_PORT },
            { "enableSSL", Constants.Settings.APP_INCOMING_SERVER_SSL },
            { "folder", Constants.Settings.APP_INCOMING_SERVER_FOLDER },
            { "pollInterval", Constants.Settings.APP_INCOMING_SERVER_POLL_INTERVAL },
            { "username", Constants.Settings.APP_INCOMING_SERVER_USERNAME },
            { "password", Constants.Settings.APP_INCOMING_SERVER_PASSWORD },
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
            return SkillResult.Error("No IMAP settings parameters provided to update.");

        await _unitOfWork.CompleteAsync();

        return SkillResult.SuccessResult(
            new { UpdatedFields = updatedFields },
            $"IMAP settings updated ({updatedFields.Count} fields): {string.Join(", ", updatedFields)}");
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
