// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("test_smtp_connection")]
public class TestSmtpConnectionSkill : BaseSkillImplementation
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IEmailTestService _emailTestService;

    public TestSmtpConnectionSkill(
        ISettingsRepository settingsRepository,
        IEmailTestService emailTestService)
    {
        _settingsRepository = settingsRepository;
        _emailTestService = emailTestService;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var server = await GetSettingValue(Constants.Settings.APP_OUTGOING_SERVER);
        var port = await GetSettingValue(Constants.Settings.APP_OUTGOING_SERVER_PORT);
        var timeout = await GetSettingValue(Constants.Settings.APP_OUTGOING_SERVER_TIMEOUT);
        var enableSsl = await GetSettingValue(Constants.Settings.APP_ENABLE_SSL);
        var authType = await GetSettingValue(Constants.Settings.APP_AUTHENTICATION_TYPE);
        var username = await GetSettingValue(Constants.Settings.APP_OUTGOING_SERVER_USERNAME);
        var password = await GetSettingValue(Constants.Settings.APP_OUTGOING_SERVER_PASSWORD);

        if (string.IsNullOrWhiteSpace(server))
        {
            return SkillResult.Error("SMTP server not configured. Please set up email settings first.");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return SkillResult.Error(
                "SMTP password not configured. Please enter it in the Email Settings UI (Settings > Email > Password).");
        }

        var timeoutMs = int.TryParse(timeout, out var t) ? t : 10000;

        var request = new EmailTestRequest
        {
            Server = server,
            Port = port,
            Username = username,
            Password = password,
            EnableSSL = string.Equals(enableSsl, "true", StringComparison.OrdinalIgnoreCase),
            AuthenticationType = authType,
            Timeout = timeoutMs
        };

        var result = await _emailTestService.TestConnectionAsync(request);

        var resultData = new
        {
            result.Success,
            result.Message,
            result.ErrorDetails,
            TestedServer = server,
            TestedPort = port,
            TestedSSL = enableSsl,
            TestedAuthType = authType,
            TestedUsername = username
        };

        return result.Success
            ? SkillResult.SuccessResult(resultData, result.Message)
            : SkillResult.Error(
                $"SMTP test failed: {result.Message}" +
                (result.ErrorDetails != null ? $" Details: {result.ErrorDetails}" : ""),
                new Dictionary<string, object> { { "details", resultData } });
    }

    private async Task<string> GetSettingValue(string settingType)
    {
        var setting = await _settingsRepository.GetSetting(settingType);
        return setting?.Value ?? "";
    }
}
