// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared staged by-name resolution used by the entity resolvers (group, contract, macro,
/// absence type, qualification). Matching stages, in order: exact normalized equality, forward
/// substring (the stored name contains the query), compact equality (all spaces removed on both
/// sides and optional label words stripped from the query, so "Makro-All-Shift" and "All Shift"
/// both resolve the camel-case macro "AllShift" — normalization drops hyphens without a
/// separator, which no token stage can bridge), fuzzy token cover (every word of the stored
/// name appears — possibly slightly damaged — in the query, which absorbs label words like
/// "Gruppe "/"Vertrag "), and whole-string fuzzy equality. A token-covered match is only
/// auto-picked when every other covered candidate is a strict token subset of it; otherwise the
/// candidates are reported for disambiguation. It never silently picks one of several equally
/// plausible matches.
/// </summary>
/// <param name="labelWords">Optional type-label words ("makro", "macro") stripped from the
/// compact query as prefix or suffix, because spoken queries often decorate the entity name
/// with its type.</param>

namespace Klacks.Api.Application.Skills;

internal static class NameResolution
{
    public static NameResolutionResult<T> Resolve<T>(
        IReadOnlyList<T> items,
        Func<T, string> nameSelector,
        string? query,
        IReadOnlyList<string>? labelWords = null)
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

        var compactVariants = CompactQueryVariants(normalizedQuery, labelWords);
        var compactMatches = named
            .Where(x =>
            {
                var compactName = CompactOf(x.Normalized);
                return compactVariants.Any(v => compactName == v || NameMatching.FuzzyEquals(compactName, v));
            })
            .ToList();
        if (compactMatches.Count > 0)
        {
            return FromStage(compactMatches);
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

    // Multilingual entities (absence types, qualifications) carry one name per language plus
    // abbreviations: every non-empty variant competes in the staged matcher, and candidates
    // pointing at the same entity collapse back into a single match.
    public static NameResolutionResult<T> ResolveVariants<T>(
        IReadOnlyList<T> items,
        Func<T, IEnumerable<string?>> variantsSelector,
        string? query,
        IReadOnlyList<string>? labelWords = null)
        where T : class
    {
        var variants = items
            .SelectMany(item => variantsSelector(item)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => new NameVariant<T>(item, value!)))
            .ToList();

        var resolution = Resolve(variants, variant => variant.Value, query, labelWords);
        if (resolution.Match != null)
        {
            return NameResolutionResult<T>.Single(resolution.Match.Item);
        }

        if (resolution.Candidates.Count > 0)
        {
            var distinct = resolution.Candidates.Select(variant => variant.Item).Distinct().ToList();
            return distinct.Count == 1
                ? NameResolutionResult<T>.Single(distinct[0])
                : NameResolutionResult<T>.Ambiguous(distinct);
        }

        return NameResolutionResult<T>.NotFound();
    }

    private sealed record NameVariant<T>(T Item, string Value)
        where T : class;

    private static NameResolutionResult<T> FromStage<T>(List<(T Item, string Normalized)> matches)
        where T : class
    {
        return matches.Count == 1
            ? NameResolutionResult<T>.Single(matches[0].Item)
            : NameResolutionResult<T>.Ambiguous(matches.Select(m => m.Item).ToList());
    }

    private static string CompactOf(string normalized)
    {
        return normalized.Replace(" ", string.Empty, StringComparison.Ordinal);
    }

    private static List<string> CompactQueryVariants(string normalizedQuery, IReadOnlyList<string>? labelWords)
    {
        var compact = CompactOf(normalizedQuery);
        var variants = new List<string> { compact };
        if (labelWords == null)
        {
            return variants;
        }

        foreach (var labelWord in labelWords)
        {
            var label = CompactOf(NameMatching.Normalize(labelWord));
            if (label.Length == 0 || compact.Length <= label.Length)
            {
                continue;
            }

            if (compact.StartsWith(label, StringComparison.Ordinal))
            {
                variants.Add(compact[label.Length..]);
            }

            if (compact.EndsWith(label, StringComparison.Ordinal))
            {
                variants.Add(compact[..^label.Length]);
            }
        }

        return variants.Distinct(StringComparer.Ordinal).ToList();
    }
}
