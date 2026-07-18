// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads the ACTIVE_INDUSTRIES setting and normalizes its comma-separated industry slugs to
/// lowercase. Returns null when the setting is missing or blank, meaning all industries are active.
/// </summary>
/// <param name="settingsReader">Read-only settings access used to load the setting value</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Settings;

namespace Klacks.Api.Infrastructure.Services.Settings;

public class ActiveIndustriesProvider : IActiveIndustriesProvider
{
    private const char SlugSeparator = ',';

    private readonly ISettingsReader _settingsReader;

    public ActiveIndustriesProvider(ISettingsReader settingsReader)
    {
        _settingsReader = settingsReader;
    }

    public async Task<IReadOnlyCollection<string>?> GetActiveIndustrySlugsAsync()
    {
        var setting = await _settingsReader.GetSetting(SettingKeys.ActiveIndustries);
        if (string.IsNullOrWhiteSpace(setting?.Value))
        {
            return null;
        }

        var slugs = setting.Value
            .Split(SlugSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(slug => slug.ToLowerInvariant())
            .Distinct()
            .ToList();

        return slugs.Count > 0 ? slugs : null;
    }
}
