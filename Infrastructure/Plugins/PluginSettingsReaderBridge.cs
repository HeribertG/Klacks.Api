// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Bridges the Contracts IPluginSettingsReader to the Core ISettingsReader.
/// </summary>

using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Plugin.Contracts;

namespace Klacks.Api.Infrastructure.Plugins;

public class PluginSettingsReaderBridge : IPluginSettingsReader
{
    private readonly ISettingsReader _settingsReader;

    public PluginSettingsReaderBridge(ISettingsReader settingsReader)
    {
        _settingsReader = settingsReader;
    }

    public async Task<string?> GetSettingAsync(string key)
    {
        var setting = await _settingsReader.GetSetting(key);
        return setting?.Value;
    }

    public async Task<int> GetSettingIntAsync(string key, int defaultValue)
    {
        var setting = await _settingsReader.GetSetting(key);
        return int.TryParse(setting?.Value, out var result) ? result : defaultValue;
    }
}
