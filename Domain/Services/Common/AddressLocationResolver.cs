// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves country and state filters by code or by any language variant of the name (states and countries tables)
/// to the codes stored on addresses. The states are read once per scope; the country goes through ICountryResolver.
/// Scoped and database-backed, so callers must not run its lookups in parallel.
/// </summary>
/// <param name="stateRepository">Loads the states (canton, Bundesland, département, ...) of the installation</param>
/// <param name="countryResolver">Resolves a country by code or any language name</param>

using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Geo;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Domain.Services.Geo;

namespace Klacks.Api.Domain.Services.Common;

public sealed class AddressLocationResolver : IAddressLocationResolver
{
    private readonly IStateRepository _stateRepository;
    private readonly ICountryResolver _countryResolver;
    private List<State>? _cachedStates;

    public AddressLocationResolver(IStateRepository stateRepository, ICountryResolver countryResolver)
    {
        _stateRepository = stateRepository;
        _countryResolver = countryResolver;
    }

    public async Task<AddressLocationCriteria> ResolveAsync(
        string? country, string? state, string? city, string? zipPrefix, CancellationToken cancellationToken = default)
    {
        var countryCode = await ResolveCountryCodeAsync(country, cancellationToken);

        IReadOnlyList<StateCodeMatch> states = string.IsNullOrWhiteSpace(state)
            ? []
            : StateCodeMatcher.Match(await LoadStatesAsync(), state, countryCode);

        return new AddressLocationCriteria(countryCode, states, Clean(city), Clean(zipPrefix));
    }

    public async Task<string?> ResolveStateCodeAsync(string? state, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            return null;
        }

        return StateCodeMatcher.Match(await LoadStatesAsync(), state, null)[0].Code;
    }

    private async Task<string?> ResolveCountryCodeAsync(string? country, CancellationToken cancellationToken)
    {
        var trimmed = Clean(country);
        if (trimmed == null)
        {
            return null;
        }

        var resolved = await _countryResolver.ResolveAsync(trimmed, cancellationToken);
        return resolved?.Abbreviation ?? trimmed;
    }

    private async Task<List<State>> LoadStatesAsync()
    {
        _cachedStates ??= await _stateRepository.List();
        return _cachedStates;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
