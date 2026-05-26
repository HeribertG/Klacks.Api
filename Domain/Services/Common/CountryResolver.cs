// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves a country by ISO abbreviation or any language name variant, and reads the
/// configured default country from the APP_ADDRESS_COUNTRY application setting.
/// </summary>
/// <param name="countryRepository">Repository for loading all countries</param>
/// <param name="settingsReader">Narrow reader for application settings</param>

using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Domain.Services.Common;

public class CountryResolver : ICountryResolver
{
    private const string AppAddressCountryKey = "APP_ADDRESS_COUNTRY";

    private readonly ICountryRepository _countryRepository;
    private readonly ISettingsReader _settingsReader;
    private List<Countries>? _cachedCountries;

    public CountryResolver(ICountryRepository countryRepository, ISettingsReader settingsReader)
    {
        _countryRepository = countryRepository;
        _settingsReader = settingsReader;
    }

    public async Task<Countries?> ResolveAsync(string? nameOrCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(nameOrCode))
        {
            return null;
        }

        _cachedCountries ??= await _countryRepository.List();

        return _cachedCountries.FirstOrDefault(c =>
            string.Equals(c.Abbreviation, nameOrCode, StringComparison.OrdinalIgnoreCase)
            || c.Name.ToDictionary().Values.Any(name =>
                !string.IsNullOrWhiteSpace(name) &&
                string.Equals(name, nameOrCode, StringComparison.OrdinalIgnoreCase)));
    }

    public async Task<Countries?> GetDefaultAsync(CancellationToken ct = default)
    {
        var setting = await _settingsReader.GetSetting(AppAddressCountryKey);
        if (setting == null || string.IsNullOrWhiteSpace(setting.Value))
        {
            return null;
        }

        return await ResolveAsync(setting.Value, ct);
    }
}
