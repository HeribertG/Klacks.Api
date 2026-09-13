// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Maps an ISO 3166-1 country code (as stored in the app owner's address settings, in either the
/// alpha-2 or the alpha-3 spelling) to its IANA time zone, for countries that use a single zone
/// nationwide. Returns null for unknown codes so callers can fall back to a default.
///
/// Countries whose territory spans several zones in daily use are deliberately NOT guessed: a
/// capital-city zone would put a Californian or Western Australian installation three hours out
/// without any signal that something is wrong. <see cref="IsMultiZoneCountry"/> names them, so a
/// caller (ICompanyClock) can report "no zone configured, and this country has no single one" as a
/// distinct state instead of a silent UTC fallback.
///
/// Islands far from the mainland (the Canaries, the Azores, Easter Island) are the documented
/// exception: the mainland zone is used, matching how those installations are actually operated.
/// NZ is the one country with a second zone that is still mapped: the Chatham Islands carry a few
/// hundred inhabitants and no installation, so the mainland zone is used rather than refusing the
/// country outright.
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
            ["ME"] = "Europe/Podgorica",
            ["AD"] = "Europe/Andorra",
            ["MC"] = "Europe/Monaco",
            ["SM"] = "Europe/San_Marino",
            ["MD"] = "Europe/Chisinau",
            ["BY"] = "Europe/Minsk",
            ["UA"] = "Europe/Kyiv",
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
            ["IR"] = "Asia/Tehran",
            ["IQ"] = "Asia/Baghdad",
            ["EG"] = "Africa/Cairo",
            ["MA"] = "Africa/Casablanca",
            ["DZ"] = "Africa/Algiers",
            ["TN"] = "Africa/Tunis",
            ["NG"] = "Africa/Lagos",
            ["KE"] = "Africa/Nairobi",
            ["ZA"] = "Africa/Johannesburg",
            ["IN"] = "Asia/Kolkata",
            ["PK"] = "Asia/Karachi",
            ["BD"] = "Asia/Dhaka",
            ["NP"] = "Asia/Kathmandu",
            ["LK"] = "Asia/Colombo",
            ["JP"] = "Asia/Tokyo",
            ["KR"] = "Asia/Seoul",
            ["TH"] = "Asia/Bangkok",
            ["VN"] = "Asia/Ho_Chi_Minh",
            ["MY"] = "Asia/Kuala_Lumpur",
            ["SG"] = "Asia/Singapore",
            ["TW"] = "Asia/Taipei",
            ["HK"] = "Asia/Hong_Kong",
            ["CN"] = "Asia/Shanghai",
            ["PH"] = "Asia/Manila",
            ["NZ"] = "Pacific/Auckland",
            ["AR"] = "America/Argentina/Buenos_Aires",
            ["CL"] = "America/Santiago",
            ["CO"] = "America/Bogota",
            ["PE"] = "America/Lima"
        };

    private static readonly IReadOnlySet<string> MultiZoneCountries =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "US",
            "CA",
            "MX",
            "BR",
            "AU",
            "RU",
            "KZ",
            "CD",
            "MN",
            "GL",
            "ID"
        };

    /// <summary>Returns the IANA time zone for the country code, or null when the code is unknown.</summary>
    public static string? Resolve(string? countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            return null;
        }

        var alpha2 = CountryCodeNormalizer.ToAlpha2(countryCode)!;
        return Map.TryGetValue(alpha2, out var timeZone) ? timeZone : null;
    }

    /// <summary>
    /// Returns true when the country spans several time zones in daily use and therefore has no single
    /// zone that could be derived from the country alone. Disjoint from <see cref="Resolve"/> by
    /// construction: a country is either mapped or declared multi-zone, never both.
    /// </summary>
    public static bool IsMultiZoneCountry(string? countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            return false;
        }

        return MultiZoneCountries.Contains(CountryCodeNormalizer.ToAlpha2(countryCode)!);
    }

    /// <summary>Every country code that resolves to a single nationwide zone.</summary>
    public static IReadOnlyCollection<string> MappedCountryCodes { get; } = Map.Keys.ToArray();

    /// <summary>Every country code declared as spanning several zones.</summary>
    public static IReadOnlyCollection<string> DeclaredMultiZoneCountryCodes => MultiZoneCountries;
}
