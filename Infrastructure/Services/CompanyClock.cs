// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves the company's current calendar date using its configured time zone, so business dates such
/// as membership ValidFrom reflect the operator's local day rather than the server's UTC day. Resolution
/// order: the explicit APP_ADDRESS_TIMEZONE setting → the IANA zone derived from APP_ADDRESS_COUNTRY →
/// UTC as the neutral fallback (never a hard-coded regional default). The returned value is the local
/// date marked as UTC-midnight (DateTimeKind.Utc), matching how a user-typed date is stored.
/// @param settingsReader - reads the APP_ADDRESS_TIMEZONE / APP_ADDRESS_COUNTRY company settings
/// @param timeProvider - supplies the current UTC instant (injected for deterministic testing)
/// </summary>

using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Settings;
using SettingsConstants = Klacks.Api.Application.Constants.Settings;

namespace Klacks.Api.Infrastructure.Services;

public class CompanyClock : ICompanyClock
{
    private readonly ISettingsReader _settingsReader;
    private readonly TimeProvider _timeProvider;

    public CompanyClock(ISettingsReader settingsReader, TimeProvider timeProvider)
    {
        _settingsReader = settingsReader;
        _timeProvider = timeProvider;
    }

    public async Task<DateTime> GetTodayAsync(CancellationToken cancellationToken = default)
    {
        var timeZone = await ResolveTimeZoneAsync();
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var localDate = TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZone).Date;
        return DateTime.SpecifyKind(localDate, DateTimeKind.Utc);
    }

    private async Task<TimeZoneInfo> ResolveTimeZoneAsync()
    {
        var explicitId = (await _settingsReader.GetSetting(SettingsConstants.APP_ADDRESS_TIMEZONE))?.Value;
        if (TryGetTimeZone(explicitId, out var explicitZone))
        {
            return explicitZone!;
        }

        var country = (await _settingsReader.GetSetting(SettingsConstants.APP_ADDRESS_COUNTRY))?.Value;
        if (TryGetTimeZone(CountryTimeZones.Resolve(country), out var countryZone))
        {
            return countryZone!;
        }

        return TimeZoneInfo.Utc;
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
