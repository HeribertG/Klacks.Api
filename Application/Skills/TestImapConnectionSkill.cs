// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Email;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class TestImapConnectionSkill : BaseSkill
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IImapTestService _imapTestService;

    public override string Name => "test_imap_connection";

    public override string Description =>
        "Tests the IMAP connection using the currently saved IMAP settings. " +
        "The password must already be entered in the settings UI. " +
        "Returns success, error details, and message count.";

    public override SkillCategory Category => SkillCategory.Query;

    public override IReadOnlyList<string> RequiredPermissions => new[] { "CanEditSettings" };

    public override IReadOnlyList<SkillParameter> Parameters => Array.Empty<SkillParameter>();

    public TestImapConnectionSkill(
        ISettingsRepository settingsRepository,
        IImapTestService imapTestService)
    {
        _settingsRepository = settingsRepository;
        _imapTestService = imapTestService;
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
        var username = await GetSettingValue(Constants.Settings.APP_INCOMING_SERVER_USERNAME);
        var password = await GetSettingValue(Constants.Settings.APP_INCOMING_SERVER_PASSWORD);

        if (string.IsNullOrWhiteSpace(server))
        {
            return SkillResult.Error("IMAP server not configured. Please set up IMAP settings first.");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return SkillResult.Error(
                "IMAP password not configured. Please enter it in the IMAP Settings UI (Settings > IMAP > Password).");
        }

        var request = new ImapTestRequest
        {
            Server = server,
            Port = int.TryParse(port, out var parsedPort) ? parsedPort : 993,
            Username = username,
            Password = password,
            EnableSSL = string.Equals(enableSsl, "true", StringComparison.OrdinalIgnoreCase),
            Folder = string.IsNullOrWhiteSpace(folder) ? "INBOX" : folder
        };

        var result = await _imapTestService.TestConnectionAsync(request);

        var resultData = new
        {
            result.Success,
            result.Message,
            result.ErrorDetails,
            result.MessageCount,
            TestedServer = server,
            TestedPort = port,
            TestedSSL = enableSsl,
            TestedFolder = request.Folder,
            TestedUsername = username
        };

        return result.Success
            ? SkillResult.SuccessResult(resultData, result.Message)
            : SkillResult.SuccessResult(resultData,
                $"IMAP test failed: {result.Message}" +
                (result.ErrorDetails != null ? $" Details: {result.ErrorDetails}" : ""));
    }

    private async Task<string> GetSettingValue(string settingType)
    {
        var setting = await _settingsRepository.GetSetting(settingType);
        return setting?.Value ?? "";
    }
}
