// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Maps an ISO 3166-1 alpha-2 country code (as stored in the app owner's address settings) to its IANA
/// time zone, for the single-time-zone countries the application operates in. Returns null for unknown
/// codes so callers can fall back to a default.
/// </summary>

namespace Klacks.Api.Application.Constants;

public static class CountryTimeZones
{
    private static readonly IReadOnlyDictionary<string, string> Map =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["CH"] = "Europe/Zurich",
            ["LI"] = "Europe/Vaduz",
            ["DE"] = "Europe/Berlin",
            ["AT"] = "Europe/Vienna",
            ["FR"] = "Europe/Paris",
            ["IT"] = "Europe/Rome"
        };

    /// <summary>Returns the IANA time zone for the country code, or null when the code is unknown.</summary>
    public static string? Resolve(string? countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            return null;
        }

        return Map.TryGetValue(countryCode.Trim(), out var timeZone) ? timeZone : null;
    }
}
