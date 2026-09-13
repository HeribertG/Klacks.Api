// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Normalises an ISO 3166-1 country code to its alpha-2 form, so lookups keyed by alpha-2 also accept
/// the alpha-3 spelling that parts of the installation data use (the seeded country table stores the
/// United States as "USA", and deploy/onprem/regions/us.json repeats it). Without this an alpha-3 code
/// silently resolved to nothing, which for a time zone meant an installation running on UTC without
/// anybody being told. Codes that are already alpha-2, unknown codes and blank input are returned
/// trimmed and unchanged, so callers keep their existing "unknown code" behaviour.
/// </summary>

namespace Klacks.Api.Application.Constants;

public static class CountryCodeNormalizer
{
    private const int Alpha3Length = 3;

    private static readonly IReadOnlyDictionary<string, string> Alpha3ToAlpha2 =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["CHE"] = "CH",
            ["LIE"] = "LI",
            ["DEU"] = "DE",
            ["AUT"] = "AT",
            ["FRA"] = "FR",
            ["ITA"] = "IT",
            ["BEL"] = "BE",
            ["NLD"] = "NL",
            ["LUX"] = "LU",
            ["DNK"] = "DK",
            ["NOR"] = "NO",
            ["SWE"] = "SE",
            ["FIN"] = "FI",
            ["ISL"] = "IS",
            ["IRL"] = "IE",
            ["GBR"] = "GB",
            ["ESP"] = "ES",
            ["PRT"] = "PT",
            ["POL"] = "PL",
            ["CZE"] = "CZ",
            ["SVK"] = "SK",
            ["HUN"] = "HU",
            ["SVN"] = "SI",
            ["HRV"] = "HR",
            ["ROU"] = "RO",
            ["BGR"] = "BG",
            ["GRC"] = "GR",
            ["EST"] = "EE",
            ["LVA"] = "LV",
            ["LTU"] = "LT",
            ["MLT"] = "MT",
            ["CYP"] = "CY",
            ["ALB"] = "AL",
            ["SRB"] = "RS",
            ["BIH"] = "BA",
            ["MKD"] = "MK",
            ["AND"] = "AD",
            ["MCO"] = "MC",
            ["SMR"] = "SM",
            ["MNE"] = "ME",
            ["MDA"] = "MD",
            ["BLR"] = "BY",
            ["UKR"] = "UA",
            ["TUR"] = "TR",
            ["ISR"] = "IL",
            ["SAU"] = "SA",
            ["ARE"] = "AE",
            ["QAT"] = "QA",
            ["KWT"] = "KW",
            ["BHR"] = "BH",
            ["OMN"] = "OM",
            ["JOR"] = "JO",
            ["LBN"] = "LB",
            ["IRN"] = "IR",
            ["IRQ"] = "IQ",
            ["EGY"] = "EG",
            ["MAR"] = "MA",
            ["DZA"] = "DZ",
            ["TUN"] = "TN",
            ["NGA"] = "NG",
            ["KEN"] = "KE",
            ["ZAF"] = "ZA",
            ["IND"] = "IN",
            ["PAK"] = "PK",
            ["BGD"] = "BD",
            ["NPL"] = "NP",
            ["LKA"] = "LK",
            ["JPN"] = "JP",
            ["KOR"] = "KR",
            ["THA"] = "TH",
            ["VNM"] = "VN",
            ["IDN"] = "ID",
            ["MYS"] = "MY",
            ["SGP"] = "SG",
            ["TWN"] = "TW",
            ["HKG"] = "HK",
            ["CHN"] = "CN",
            ["PHL"] = "PH",
            ["NZL"] = "NZ",
            ["ARG"] = "AR",
            ["CHL"] = "CL",
            ["COL"] = "CO",
            ["PER"] = "PE",
            ["USA"] = "US",
            ["CAN"] = "CA",
            ["MEX"] = "MX",
            ["BRA"] = "BR",
            ["AUS"] = "AU",
            ["RUS"] = "RU",
            ["KAZ"] = "KZ",
            ["COD"] = "CD",
            ["MNG"] = "MN",
            ["GRL"] = "GL"
        };

    /// <summary>
    /// Every alpha-2 code reachable through an alpha-3 spelling, so a guard test can prove that no
    /// country known to CountryTimeZones is missing its alpha-3 entry.
    /// </summary>
    public static IReadOnlyCollection<string> Alpha2CodesReachableFromAlpha3 { get; } = Alpha3ToAlpha2.Values.ToArray();

    /// <summary>
    /// Returns the alpha-2 code for an alpha-3 input, otherwise the trimmed input unchanged.
    /// </summary>
    public static string? ToAlpha2(string? countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            return countryCode;
        }

        var trimmed = countryCode.Trim();
        if (trimmed.Length != Alpha3Length)
        {
            return trimmed;
        }

        return Alpha3ToAlpha2.TryGetValue(trimmed, out var alpha2) ? alpha2 : trimmed;
    }
}
