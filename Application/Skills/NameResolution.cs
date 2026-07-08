// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared staged by-name resolution used by the group and contract resolvers. Matching stages,
/// in order: exact normalized equality, forward substring (the stored name contains the query),
/// fuzzy token cover (every word of the stored name appears — possibly slightly damaged — in the
/// query, which absorbs label words like "Gruppe "/"Vertrag "), and whole-string fuzzy equality.
/// A token-covered match is only auto-picked when every other covered candidate is a strict
/// token subset of it; otherwise the candidates are reported for disambiguation. It never
/// silently picks one of several equally plausible matches.
/// </summary>

namespace Klacks.Api.Application.Skills;

internal static class NameResolution
{
    public static NameResolutionResult<T> Resolve<T>(
        IReadOnlyList<T> items,
        Func<T, string> nameSelector,
        string? query)
        where T : class
    {
        var normalizedQuery = NameMatching.Normalize(query);
        if (normalizedQuery.Length == 0)
        {
            return NameResolutionResult<T>.NotFound();
        }

        var named = items
            .Select(item => (Item: item, Normalized: NameMatching.Normalize(nameSelector(item))))
            .Where(x => x.Normalized.Length > 0)
            .ToList();

        var exact = named.Where(x => x.Normalized == normalizedQuery).ToList();
        if (exact.Count > 0)
        {
            return FromStage(exact);
        }

        var forward = named
            .Where(x => x.Normalized.Contains(normalizedQuery, StringComparison.Ordinal))
            .ToList();
        if (forward.Count > 0)
        {
            return FromStage(forward);
        }

        var queryTokens = NameMatching.Tokenize(normalizedQuery);
        var covered = named
            .Select(x => (x.Item, x.Normalized, Tokens: NameMatching.Tokenize(x.Normalized)))
            .Where(x => NameMatching.TokensFuzzyCovered(x.Tokens, queryTokens))
            .OrderByDescending(x => x.Tokens.Length)
            .ThenByDescending(x => x.Normalized.Length)
            .ToList();
        if (covered.Count == 1)
        {
            return NameResolutionResult<T>.Single(covered[0].Item);
        }

        if (covered.Count > 1)
        {
            var best = covered[0];
            var bestTokens = new HashSet<string>(best.Tokens, StringComparer.Ordinal);
            var othersAreStrictSubsets = covered
                .Skip(1)
                .All(x => x.Tokens.Length < best.Tokens.Length
                          && x.Tokens.All(bestTokens.Contains));
            return othersAreStrictSubsets
                ? NameResolutionResult<T>.Single(best.Item)
                : NameResolutionResult<T>.Ambiguous(covered.Select(x => x.Item).ToList());
        }

        var fuzzy = named
            .Where(x => NameMatching.FuzzyEquals(x.Normalized, normalizedQuery))
            .ToList();
        if (fuzzy.Count > 0)
        {
            return FromStage(fuzzy);
        }

        return NameResolutionResult<T>.NotFound();
    }

    private static NameResolutionResult<T> FromStage<T>(List<(T Item, string Normalized)> matches)
        where T : class
    {
        return matches.Count == 1
            ? NameResolutionResult<T>.Single(matches[0].Item)
            : NameResolutionResult<T>.Ambiguous(matches.Select(m => m.Item).ToList());
    }
}
