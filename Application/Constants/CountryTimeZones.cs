// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Maps an ISO 3166-1 alpha-2 country code (as stored in the app owner's address settings) to its IANA
/// time zone, for the countries the application operates in. For countries spanning multiple time zones
/// the capital-city zone is used. Returns null for unknown codes so callers can fall back to a default.
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
            ["IT"] = "Europe/Rome",
            ["BE"] = "Europe/Brussels",
            ["NL"] = "Europe/Amsterdam",
            ["LU"] = "Europe/Luxembourg",
            ["DK"] = "Europe/Copenhagen",
            ["NO"] = "Europe/Oslo",
            ["SE"] = "Europe/Stockholm",
            ["FI"] = "Europe/Helsinki",
            ["IS"] = "Atlantic/Reykjavik",
            ["IE"] = "Europe/Dublin",
            ["GB"] = "Europe/London",
            ["ES"] = "Europe/Madrid",
            ["PT"] = "Europe/Lisbon",
            ["PL"] = "Europe/Warsaw",
            ["CZ"] = "Europe/Prague",
            ["SK"] = "Europe/Bratislava",
            ["HU"] = "Europe/Budapest",
            ["SI"] = "Europe/Ljubljana",
            ["HR"] = "Europe/Zagreb",
            ["RO"] = "Europe/Bucharest",
            ["BG"] = "Europe/Sofia",
            ["GR"] = "Europe/Athens",
            ["EE"] = "Europe/Tallinn",
            ["LV"] = "Europe/Riga",
            ["LT"] = "Europe/Vilnius",
            ["MT"] = "Europe/Malta",
            ["CY"] = "Asia/Nicosia",
            ["AL"] = "Europe/Tirane",
            ["RS"] = "Europe/Belgrade",
            ["BA"] = "Europe/Sarajevo",
            ["MK"] = "Europe/Skopje",
            ["TR"] = "Europe/Istanbul",
            ["IL"] = "Asia/Jerusalem",
            ["SA"] = "Asia/Riyadh",
            ["AE"] = "Asia/Dubai",
            ["QA"] = "Asia/Qatar",
            ["KW"] = "Asia/Kuwait",
            ["BH"] = "Asia/Bahrain",
            ["OM"] = "Asia/Muscat",
            ["JO"] = "Asia/Amman",
            ["LB"] = "Asia/Beirut",
            ["EG"] = "Africa/Cairo",
            ["JP"] = "Asia/Tokyo",
            ["KR"] = "Asia/Seoul",
            ["TH"] = "Asia/Bangkok",
            ["VN"] = "Asia/Ho_Chi_Minh",
            ["ID"] = "Asia/Jakarta",
            ["MY"] = "Asia/Kuala_Lumpur",
            ["SG"] = "Asia/Singapore",
            ["TW"] = "Asia/Taipei",
            ["CN"] = "Asia/Shanghai"
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
