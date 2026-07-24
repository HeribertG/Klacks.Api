// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Parses and validates the flat calendar-token notation used by the calendar-selection skills:
/// a bare country code (e.g. "US") addresses the national calendar, "COUNTRY-STATE" (e.g.
/// "CH-BE") addresses a subdivision. Every token is validated against the real country/state
/// list (<see cref="StateCountryToken"/>) so the skills never invent a calendar that does not
/// exist; the resolved Country/State pair uses the list's canonical casing.
/// </summary>

using Klacks.Api.Domain.DTOs.Filter;

namespace Klacks.Api.Application.Skills;

internal static class CalendarTokenParser
{
    private const char Separator = '-';

    public static string Format(string country, string state) =>
        string.Equals(country, state, StringComparison.OrdinalIgnoreCase) ? country : $"{country}-{state}";

    public static (string Country, string State, string? Error) Resolve(
        string rawToken, IReadOnlyList<StateCountryToken> availableTokens)
    {
        var trimmed = (rawToken ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return (string.Empty, string.Empty,
                "Calendar tokens must not be empty. Use a country code (e.g. 'US') or 'COUNTRY-STATE' (e.g. 'CH-BE').");
        }

        var upper = trimmed.ToUpperInvariant();
        var separatorIndex = upper.IndexOf(Separator);
        var country = separatorIndex < 0 ? upper : upper[..separatorIndex];
        var state = separatorIndex < 0 ? upper : upper[(separatorIndex + 1)..];

        if (country.Length == 0 || state.Length == 0)
        {
            return (string.Empty, string.Empty,
                $"'{rawToken}' is not a valid calendar token. Use a country code (e.g. 'US') or 'COUNTRY-STATE' (e.g. 'CH-BE').");
        }

        var match = availableTokens.FirstOrDefault(t =>
            string.Equals(t.Country, country, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(t.State, state, StringComparison.OrdinalIgnoreCase));

        if (match == null)
        {
            return (string.Empty, string.Empty,
                $"'{rawToken}' does not match a known country or subdivision. Use a country code (e.g. 'US') " +
                "or 'COUNTRY-STATE' (e.g. 'CH-BE') — use list_calendars or the calendar rules settings to find valid codes.");
        }

        return (match.Country, match.State, null);
    }
}
