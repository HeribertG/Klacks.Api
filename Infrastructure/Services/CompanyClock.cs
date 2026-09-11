// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves the company's current time using its configured time zone, so business dates and "now"
/// reflect the operator's own local day and clock rather than the server's UTC day. Resolution order:
/// the explicit APP_ADDRESS_TIMEZONE setting -> the IANA zone derived from APP_ADDRESS_COUNTRY -> the
/// IANA zone derived from the global calendar's country setting (SettingKeys.GlobalCalendarCountry,
/// used by installations that only configured a holiday calendar, not an address) -> UTC as the neutral
/// fallback (never a hard-coded regional default). The resolved zone is memoised for the
/// lifetime of this scoped instance, stamped with ISettingsChangeVersion.Current so a settings write
/// earlier in the same DI scope (e.g. a settings-writing skill followed by a recalculation in the same
/// chain) is picked up instead of served from a stale cache - mirroring the pattern in
/// ClientContractDataProvider.
/// @param settingsReader - reads the APP_ADDRESS_TIMEZONE / APP_ADDRESS_COUNTRY company settings
/// @param timeProvider - supplies the current UTC instant (injected for deterministic testing)
/// @param settingsChangeVersion - process-wide settings write counter used to invalidate the per-scope memo
/// </summary>

using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Settings;
using SettingsConstants = Klacks.Api.Application.Constants.Settings;

namespace Klacks.Api.Infrastructure.Services;

public class CompanyClock : ICompanyClock
{
    private readonly ISettingsReader _settingsReader;
    private readonly TimeProvider _timeProvider;
    private readonly ISettingsChangeVersion _settingsChangeVersion;

    private CompanyTimeZoneResolution? _cachedResolution;
    private long? _cachedZoneVersion;

    public CompanyClock(ISettingsReader settingsReader, TimeProvider timeProvider, ISettingsChangeVersion settingsChangeVersion)
    {
        _settingsReader = settingsReader;
        _timeProvider = timeProvider;
        _settingsChangeVersion = settingsChangeVersion;
    }

    public async Task<DateTime> GetTodayAsync(CancellationToken cancellationToken = default)
    {
        var localNow = await GetNowAsync(cancellationToken);
        return DateTime.SpecifyKind(localNow.Date, DateTimeKind.Utc);
    }

    public async Task<DateOnly> GetTodayDateAsync(CancellationToken cancellationToken = default)
    {
        var localNow = await GetNowAsync(cancellationToken);
        return DateOnly.FromDateTime(localNow.Date);
    }

    public async Task<DateTimeOffset> GetNowAsync(CancellationToken cancellationToken = default)
    {
        var zone = await GetTimeZoneAsync(cancellationToken);
        return TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), zone);
    }

    public async Task<TimeZoneInfo> GetTimeZoneAsync(CancellationToken cancellationToken = default)
    {
        var resolution = await GetTimeZoneResolutionAsync(cancellationToken);
        return resolution.Zone;
    }

    public async Task<CompanyTimeZoneResolution> GetTimeZoneResolutionAsync(CancellationToken cancellationToken = default)
    {
        var versionAtRead = _settingsChangeVersion.Current;
        if (_cachedResolution is not null && _cachedZoneVersion == versionAtRead)
        {
            return _cachedResolution;
        }

        var resolution = await ResolveTimeZoneAsync();
        _cachedResolution = resolution;
        _cachedZoneVersion = versionAtRead;
        return resolution;
    }

    private static readonly string[] ZoneSettingTypes =
    [
        SettingsConstants.APP_ADDRESS_TIMEZONE,
        SettingsConstants.APP_ADDRESS_COUNTRY,
        SettingKeys.GlobalCalendarCountry
    ];

    private async Task<CompanyTimeZoneResolution> ResolveTimeZoneAsync()
    {
        var settings = await _settingsReader.GetSettingsByTypesAsync(ZoneSettingTypes);

        if (settings.TryGetValue(SettingsConstants.APP_ADDRESS_TIMEZONE, out var explicitId)
            && TryGetTimeZone(explicitId, out var explicitZone))
        {
            return new CompanyTimeZoneResolution(explicitZone!, CompanyTimeZoneSource.Setting);
        }

        if (settings.TryGetValue(SettingsConstants.APP_ADDRESS_COUNTRY, out var country)
            && TryGetTimeZone(CountryTimeZones.Resolve(country), out var countryZone))
        {
            return new CompanyTimeZoneResolution(countryZone!, CompanyTimeZoneSource.AddressCountry);
        }

        if (settings.TryGetValue(SettingKeys.GlobalCalendarCountry, out var calendarCountry)
            && TryGetTimeZone(CountryTimeZones.Resolve(calendarCountry), out var calendarCountryZone))
        {
            return new CompanyTimeZoneResolution(calendarCountryZone!, CompanyTimeZoneSource.CalendarCountry);
        }

        return new CompanyTimeZoneResolution(TimeZoneInfo.Utc, CompanyTimeZoneSource.Utc);
    }

    private static bool TryGetTimeZone(string? timeZoneId, out TimeZoneInfo? zone)
    {
        zone = null;
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return false;
        }

        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
            return true;
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return false;
        }
    }
}
