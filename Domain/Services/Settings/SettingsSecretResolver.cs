// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves a secret that a client cannot know because the settings API masks server-only values.
/// </summary>
/// <param name="settingType">Setting key the stored secret is read from when the client sends no usable value</param>
/// <param name="providedValue">Value the client sent; the masked placeholder and empty input fall back to the stored secret</param>
/// <param name="bindings">Settings the request must still point at before a stored secret is released</param>

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Settings;

namespace Klacks.Api.Domain.Services.Settings;

public class SettingsSecretResolver : ISettingsSecretResolver
{
    private const char FullyQualifiedDomainNameSuffix = '.';

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
        if (ClientSuppliedItsOwnValue(providedValue))
        {
            return providedValue!;
        }

        return await ReadStoredSecretAsync(settingType);
    }

    public async Task<string> ResolveBoundAsync(string settingType, string? providedValue, params SecretBinding[] bindings)
    {
        if (ClientSuppliedItsOwnValue(providedValue))
        {
            return providedValue!;
        }

        foreach (var binding in bindings)
        {
            if (!await BindingMatchesStoredSettingAsync(binding))
            {
                return string.Empty;
            }
        }

        return await ReadStoredSecretAsync(settingType);
    }

    private static bool ClientSuppliedItsOwnValue(string? providedValue) =>
        !string.IsNullOrWhiteSpace(providedValue) && providedValue != SettingsMasking.MaskedValue;

    private async Task<bool> BindingMatchesStoredSettingAsync(SecretBinding binding)
    {
        var storedSetting = await _settingsReader.GetSetting(binding.SettingType);
        if (string.IsNullOrWhiteSpace(storedSetting?.Value))
        {
            return false;
        }

        return Normalize(storedSetting.Value).Equals(Normalize(binding.ProvidedValue), StringComparison.OrdinalIgnoreCase);
    }

    private async Task<string> ReadStoredSecretAsync(string settingType)
    {
        var setting = await _settingsReader.GetSetting(settingType);
        if (string.IsNullOrWhiteSpace(setting?.Value))
        {
            return string.Empty;
        }

        return _encryptionService.Decrypt(setting.Value);
    }

    private static string Normalize(string? value) =>
        (value ?? string.Empty).Trim().TrimEnd(FullyQualifiedDomainNameSuffix);
}
