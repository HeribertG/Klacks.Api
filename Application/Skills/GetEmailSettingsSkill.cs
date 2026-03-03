// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class GetEmailSettingsSkill : BaseSkill
{
    private readonly ISettingsRepository _settingsRepository;

    public override string Name => "get_email_settings";

    public override string Description =>
        "Reads the current outgoing email (SMTP) settings. " +
        "Returns server, port, timeout, SSL, authentication type, disposition notification, " +
        "read receipt, reply-to, mark and SMTP username. Does NOT return the password.";

    public override SkillCategory Category => SkillCategory.Query;

    public override IReadOnlyList<string> RequiredPermissions => new[] { "CanViewSettings" };

    public override IReadOnlyList<SkillParameter> Parameters => Array.Empty<SkillParameter>();

    public GetEmailSettingsSkill(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var outgoingServer = await GetSettingValue(Constants.Settings.APP_OUTGOING_SERVER);
        var port = await GetSettingValue(Constants.Settings.APP_OUTGOING_SERVER_PORT);
        var timeout = await GetSettingValue(Constants.Settings.APP_OUTGOING_SERVER_TIMEOUT);
        var enabledSsl = await GetSettingValue(Constants.Settings.APP_ENABLE_SSL);
        var authenticationType = await GetSettingValue(Constants.Settings.APP_AUTHENTICATION_TYPE);
        var dispositionNotification = await GetSettingValue(Constants.Settings.APP_DISPOSITION_NOTIFICATION);
        var readReceipt = await GetSettingValue(Constants.Settings.APP_READ_RECEIPT);
        var replyTo = await GetSettingValue(Constants.Settings.APP_REPLY_TO);
        var mark = await GetSettingValue(Constants.Settings.APP_MARK);
        var smtpUsername = await GetSettingValue(Constants.Settings.APP_OUTGOING_SERVER_USERNAME);

        var resultData = new
        {
            OutgoingServer = outgoingServer,
            Port = port,
            Timeout = timeout,
            EnabledSSL = enabledSsl,
            AuthenticationType = authenticationType,
            DispositionNotification = dispositionNotification,
            ReadReceipt = readReceipt,
            ReplyTo = replyTo,
            Mark = mark,
            SmtpUsername = smtpUsername
        };

        return SkillResult.SuccessResult(resultData,
            $"Email settings: Server={outgoingServer}, Port={port}, SSL={enabledSsl}, Auth={authenticationType}");
    }

    private async Task<string> GetSettingValue(string settingType)
    {
        var setting = await _settingsRepository.GetSetting(settingType);
        return setting?.Value ?? "";
    }
}
