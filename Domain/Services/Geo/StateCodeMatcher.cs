// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves what a user calls a state (canton, Bundesland, département, region, ...) to the codes stored on
/// addresses: a code match first, then a match on any language variant of the name, both case-insensitive and
/// limited to the country when one is given. Every match keeps its country, because codes repeat across countries
/// (BE = Bern/CH and Berlin/DE). Text that matches no known state is kept as typed (it may be a code the table
/// does not hold), bound to the given country if any.
/// </summary>
/// <param name="states">The installation's states (deleted rows are ignored)</param>
/// <param name="text">Code or name as the user said it</param>
/// <param name="countryCode">Country code the state must belong to; null = any country</param>

using Klacks.Api.Domain.Models.Geo;
using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Domain.Services.Geo;

public static class StateCodeMatcher
{
    public static IReadOnlyList<StateCodeMatch> Match(IEnumerable<State> states, string text, string? countryCode)
    {
        var known = FindKnown(states, text, countryCode);
        return known.Count > 0 ? known : [new StateCodeMatch(text.Trim(), countryCode)];
    }

    private static IReadOnlyList<StateCodeMatch> FindKnown(IEnumerable<State> states, string text, string? countryCode)
    {
        var trimmed = text.Trim();
        var scoped = states
            .Where(s => !s.IsDeleted && !string.IsNullOrWhiteSpace(s.Abbreviation))
            .Where(s => countryCode == null || EqualsIgnoreCase(s.CountryPrefix, countryCode))
            .ToList();

        var byCode = scoped.Where(s => EqualsIgnoreCase(s.Abbreviation, trimmed));
        var byName = scoped.Where(s => s.Name != null
            && s.Name.ToDictionary().Values.Any(name => EqualsIgnoreCase(name, trimmed)));

        return byCode
            .Concat(byName)
            .Select(s => new StateCodeMatch(s.Abbreviation.Trim(), NullIfBlank(s.CountryPrefix)))
            .Distinct()
            .ToList();
    }

    private static bool EqualsIgnoreCase(string? left, string? right)
        => string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
