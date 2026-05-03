// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves full state/region names (e.g. from Nominatim) to their abbreviation from the state table.
/// Matches against all language variants (de, en, fr, it) with case-insensitive contains matching.
/// </summary>
/// <param name="_stateRepository">Repository for state/region lookups</param>
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Domain.Services.Common;

public class StateAbbreviationResolver
{
    private readonly IStateRepository _stateRepository;
    private List<State>? _cachedStates;

    public StateAbbreviationResolver(IStateRepository stateRepository)
    {
        _stateRepository = stateRepository;
    }

    public async Task<string?> ResolveAsync(string? nominatimState)
    {
        if (string.IsNullOrWhiteSpace(nominatimState))
            return null;

        _cachedStates ??= await _stateRepository.List();

        var match = _cachedStates.FirstOrDefault(state =>
            string.Equals(state.Abbreviation, nominatimState, StringComparison.OrdinalIgnoreCase)
            || state.Name.ToDictionary().Values.Any(name =>
                string.Equals(name, nominatimState, StringComparison.OrdinalIgnoreCase)
                || nominatimState.Contains(name, StringComparison.OrdinalIgnoreCase)
                || name.Contains(nominatimState, StringComparison.OrdinalIgnoreCase)));

        return match?.Abbreviation;
    }
}
