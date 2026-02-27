// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class GetImapSettingsSkill : BaseSkill
{
    private readonly ISettingsRepository _settingsRepository;

    public override string Name => "get_imap_settings";

    public override string Description =>
        "Reads the current incoming email (IMAP) settings. " +
        "Returns server, port, SSL, folder, poll interval and username. Does NOT return the password.";

    public override SkillCategory Category => SkillCategory.Query;

    public override IReadOnlyList<string> RequiredPermissions => new[] { "CanViewSettings" };

    public override IReadOnlyList<SkillParameter> Parameters => Array.Empty<SkillParameter>();

    public GetImapSettingsSkill(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var server = await GetSettingValue(Constants.Settings.APP_INCOMING_SERVER);
        var port = await GetSettingValue(Constants.Settings.APP_INCOMING_SERVER_PORT);
        var enableSsl = await GetSettingValue(Constants.Settings.APP_INCOMING_SERVER_SSL);
        var folder = await GetSettingValue(Constants.Settings.APP_INCOMING_SERVER_FOLDER);
        var pollInterval = await GetSettingValue(Constants.Settings.APP_INCOMING_SERVER_POLL_INTERVAL);
        var username = await GetSettingValue(Constants.Settings.APP_INCOMING_SERVER_USERNAME);

        var resultData = new
        {
            Server = server,
            Port = port,
            EnableSSL = enableSsl,
            Folder = folder,
            PollInterval = pollInterval,
            Username = username
        };

        return SkillResult.SuccessResult(resultData,
            $"IMAP settings: Server={server}, Port={port}, SSL={enableSsl}, Folder={folder}");
    }

    private async Task<string> GetSettingValue(string settingType)
    {
        var setting = await _settingsRepository.GetSetting(settingType);
        return setting?.Value ?? "";
    }
}
