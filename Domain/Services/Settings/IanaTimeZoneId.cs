// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Converts a resolved TimeZoneInfo to its IANA id, even when TimeZoneInfo.FindSystemTimeZoneById
/// resolved it from a Windows time zone id (accepted on Windows hosts alongside IANA ids). Every
/// consumer that surfaces a time zone id to the browser (Intl.DateTimeFormat throws a RangeError on a
/// Windows id) or persists one for later reuse must go through this, so the whole backend agrees on one
/// id shape regardless of which id a setting, an LLM parameter, or a country lookup happened to supply.
/// </summary>

namespace Klacks.Api.Domain.Services.Settings;

public static class IanaTimeZoneId
{
    public static string From(TimeZoneInfo zone)
    {
        if (zone.HasIanaId)
        {
            return zone.Id;
        }

        return TimeZoneInfo.TryConvertWindowsIdToIanaId(zone.Id, out var ianaId) ? ianaId : zone.Id;
    }

    /// <summary>
    /// Validates an arbitrary caller-supplied id (a setting value, an LLM skill parameter, ...) and
    /// returns its IANA form, whether the id itself was already IANA or a Windows time zone id.
    /// </summary>
    public static bool TryFrom(string? timeZoneId, out string? ianaId)
    {
        ianaId = null;
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return false;
        }

        try
        {
            ianaId = From(TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim()));
            return true;
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return false;
        }
    }
}
