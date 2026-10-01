// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Splits the combined address cells of an import: "8000 Zürich", "D-80331 München" or "111 22
/// Stockholm" into postcode and locality, and joins a separate house-number column onto the street.
/// </summary>

using System.Text.RegularExpressions;

namespace Klacks.Api.Application.Services.ClientImport;

public static class ClientImportAddressSplitter
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(250);

    private static readonly Regex ZipCityPattern = new(
        @"^\s*(?:[A-Za-z]{1,3}\s?-\s?)?(?<zip>\d{3}\s\d{2}|\d{4,6}|\d{2}-\d{3})\s+(?<city>\S.*?)\s*$",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        RegexTimeout);

    public static bool TrySplitZipCity(string? value, out string zip, out string city)
    {
        zip = city = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var match = ZipCityPattern.Match(value);
        if (!match.Success)
        {
            return false;
        }

        zip = match.Groups["zip"].Value;
        city = match.Groups["city"].Value;
        return true;
    }

    public static string? CombineStreet(string? street, string? houseNumber)
    {
        var trimmedStreet = street?.Trim();
        var trimmedNumber = houseNumber?.Trim();

        if (string.IsNullOrEmpty(trimmedNumber))
        {
            return string.IsNullOrEmpty(trimmedStreet) ? null : trimmedStreet;
        }

        return string.IsNullOrEmpty(trimmedStreet) ? trimmedNumber : string.Concat(trimmedStreet, " ", trimmedNumber);
    }
}
