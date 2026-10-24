// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads GEOCODING_EMPLOYEE_ADDRESSES_ENABLED on every call. Only a value that parses as true allows employee
/// addresses to be geocoded; absent, empty or unparseable means off (privacy default, the opposite of the
/// explain-offers switch).
/// </summary>
/// <param name="settingsReader">Reads the setting row</param>

using Klacks.Api.Application.Interfaces.Geocoding;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.Services.Geocoding;

public sealed class AddressGeocodingSettings : IAddressGeocodingSettings
{
    private readonly ISettingsReader _settingsReader;

    public AddressGeocodingSettings(ISettingsReader settingsReader)
    {
        _settingsReader = settingsReader;
    }

    public async Task<bool> AreEmployeeAddressesAllowedAsync(CancellationToken cancellationToken = default)
    {
        var setting = await _settingsReader.GetSetting(SettingKeys.GeocodingEmployeeAddressesEnabled);
        return bool.TryParse(setting?.Value, out var allowed) && allowed;
    }
}
