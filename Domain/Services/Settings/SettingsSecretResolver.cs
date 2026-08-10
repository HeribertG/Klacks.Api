// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves a secret that a client cannot know because the settings API masks server-only values.
/// </summary>
/// <param name="settingType">Setting key the stored secret is read from when the client sends no usable value</param>
/// <param name="providedValue">Value the client sent; the masked placeholder and empty input fall back to the stored secret</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Settings;

namespace Klacks.Api.Domain.Services.Settings;

public class SettingsSecretResolver : ISettingsSecretResolver
{
    private readonly ISettingsReader _settingsReader;
    private readonly ISettingsEncryptionService _encryptionService;

    public SettingsSecretResolver(
        ISettingsReader settingsReader,
        ISettingsEncryptionService encryptionService)
    {
        _settingsReader = settingsReader;
        _encryptionService = encryptionService;
    }

    public async Task<string> ResolveAsync(string settingType, string? providedValue)
    {
        if (!string.IsNullOrWhiteSpace(providedValue) && providedValue != SettingsMasking.MaskedValue)
        {
            return providedValue;
        }

        var setting = await _settingsReader.GetSetting(settingType);
        if (string.IsNullOrWhiteSpace(setting?.Value))
        {
            return string.Empty;
        }

        return _encryptionService.Decrypt(setting.Value);
    }
}
