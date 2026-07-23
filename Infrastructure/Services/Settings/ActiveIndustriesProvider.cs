// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads the ACTIVE_INDUSTRIES setting and normalizes it to the industry slugs it activates.
/// Returns null when the setting is missing or blank (no filtering, all industries active),
/// an empty collection when the setting is the <see cref="IndustrySlugs.Custom"/> marker (only
/// industry-less rows active), or the normalized lowercase slug list otherwise - a single
/// selected profile going forward, or a legacy comma-separated multi-slug value from before the
/// single-profile-or-custom model.
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

        var normalizedValue = setting.Value.Trim().ToLowerInvariant();
        if (normalizedValue == IndustrySlugs.Custom)
        {
            return Array.Empty<string>();
        }

        var slugs = setting.Value
            .Split(SlugSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(slug => slug.ToLowerInvariant())
            .Distinct()
            .ToList();

        return slugs.Count > 0 ? slugs : null;
    }
}
