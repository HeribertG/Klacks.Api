// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves a full state/region name from the geocoder (Nominatim) to the abbreviation of a state of the
/// address' own country. Only states whose country prefix matches the country (alpha-2 and alpha-3 spellings
/// are treated alike) are candidates, so "Berlin" never resolves to the Swiss canton Bern. An exact match on
/// the abbreviation or on any language variant of the name wins over a partial (contains) match, so
/// "Nordrhein-Westfalen" never resolves to the French département "Nord". For countries whose Nominatim
/// "state" is a region above the state level of the state table (France: région vs. département) the
/// geocoder's "county" is used as the source instead. A blank country keeps the legacy behaviour of
/// searching every state.
/// </summary>
/// <param name="_stateRepository">Repository for state/region lookups</param>
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Domain.Services.Common;

public class StateAbbreviationResolver
{
    private static readonly HashSet<string> CountriesUsingCountyAsState = new(StringComparer.OrdinalIgnoreCase) { "FR" };

    private readonly IStateRepository _stateRepository;
    private List<State>? _cachedStates;

    public StateAbbreviationResolver(IStateRepository stateRepository)
    {
        _stateRepository = stateRepository;
    }

    /// <param name="countryCode">Country code of the address (alpha-2 or alpha-3); blank searches every country</param>
    /// <param name="nominatimState">The geocoder's "state" value</param>
    /// <param name="nominatimCounty">The geocoder's "county" value; the source for countries listed in CountriesUsingCountyAsState</param>
    public async Task<string?> ResolveAsync(string? countryCode, string? nominatimState, string? nominatimCounty = null)
    {
        var country = CountryCodeNormalizer.ToAlpha2(countryCode)?.ToUpperInvariant() ?? string.Empty;
        var source = CountriesUsingCountyAsState.Contains(country) ? nominatimCounty : nominatimState;
        if (string.IsNullOrWhiteSpace(source))
        {
            return null;
        }

        var name = source.Trim();
        _cachedStates ??= await _stateRepository.List();

        var candidates = country.Length == 0
            ? _cachedStates
            : _cachedStates
                .Where(state => string.Equals(
                    CountryCodeNormalizer.ToAlpha2(state.CountryPrefix), country, StringComparison.OrdinalIgnoreCase))
                .ToList();

        var exact = candidates.FirstOrDefault(state =>
            string.Equals(state.Abbreviation, name, StringComparison.OrdinalIgnoreCase)
            || state.Name.ToDictionary().Values.Any(value => string.Equals(value, name, StringComparison.OrdinalIgnoreCase)));
        if (exact != null)
        {
            return exact.Abbreviation;
        }

        var partial = candidates.FirstOrDefault(state =>
            state.Name.ToDictionary().Values.Any(value =>
                name.Contains(value, StringComparison.OrdinalIgnoreCase)
                || value.Contains(name, StringComparison.OrdinalIgnoreCase)));

        return partial?.Abbreviation;
    }
}
