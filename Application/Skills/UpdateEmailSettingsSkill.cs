// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class UpdateEmailSettingsSkill : BaseSkill
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IUnitOfWork _unitOfWork;

    public override string Name => "update_email_settings";

    public override string Description =>
        "Updates the outgoing email (SMTP) settings. " +
        "All parameters are optional - only provided values will be updated.";

    public override SkillCategory Category => SkillCategory.Crud;

    public override IReadOnlyList<string> RequiredPermissions => new[] { "CanEditSettings" };

    public override IReadOnlyList<SkillParameter> Parameters => new[]
    {
        new SkillParameter("outgoingServer", "SMTP server hostname", SkillParameterType.String, Required: false),
        new SkillParameter("outgoingServerPort", "SMTP server port (e.g. 587, 465)", SkillParameterType.String, Required: false),
        new SkillParameter("outgoingServerTimeout", "Connection timeout in milliseconds", SkillParameterType.String, Required: false),
        new SkillParameter("enabledSSL", "Enable SSL/TLS (true or false)", SkillParameterType.String, Required: false),
        new SkillParameter("authenticationType", "Authentication type (None, LOGIN, PLAIN, CRAM-MD5)", SkillParameterType.String, Required: false),
        new SkillParameter("dispositionNotification", "Request disposition notification (true or false)", SkillParameterType.String, Required: false),
        new SkillParameter("readReceipt", "Read receipt email address", SkillParameterType.String, Required: false),
        new SkillParameter("replyTo", "Reply-to email address", SkillParameterType.String, Required: false),
        new SkillParameter("mark", "Email mark/label", SkillParameterType.String, Required: false),
        new SkillParameter("smtpUsername", "SMTP authentication username", SkillParameterType.String, Required: false),
        new SkillParameter("smtpPassword", "SMTP authentication password", SkillParameterType.String, Required: false),
    };

    public UpdateEmailSettingsSkill(
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
            { "outgoingServer", Constants.Settings.APP_OUTGOING_SERVER },
            { "outgoingServerPort", Constants.Settings.APP_OUTGOING_SERVER_PORT },
            { "outgoingServerTimeout", Constants.Settings.APP_OUTGOING_SERVER_TIMEOUT },
            { "enabledSSL", Constants.Settings.APP_ENABLE_SSL },
            { "authenticationType", Constants.Settings.APP_AUTHENTICATION_TYPE },
            { "dispositionNotification", Constants.Settings.APP_DISPOSITION_NOTIFICATION },
            { "readReceipt", Constants.Settings.APP_READ_RECEIPT },
            { "replyTo", Constants.Settings.APP_REPLY_TO },
            { "mark", Constants.Settings.APP_MARK },
            { "smtpUsername", Constants.Settings.APP_OUTGOING_SERVER_USERNAME },
            { "smtpPassword", Constants.Settings.APP_OUTGOING_SERVER_PASSWORD },
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
            return SkillResult.Error("No email settings parameters provided to update.");

        await _unitOfWork.CompleteAsync();

        return SkillResult.SuccessResult(
            new { UpdatedFields = updatedFields },
            $"Email settings updated ({updatedFields.Count} fields): {string.Join(", ", updatedFields)}");
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
