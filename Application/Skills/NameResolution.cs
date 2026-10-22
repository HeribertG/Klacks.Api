// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Shared staged by-name resolution used by the entity resolvers (group, contract, macro,
/// absence type, qualification). Matching stages, in order: exact normalized equality, forward
/// substring (the stored name contains the query), compact equality (all spaces removed on both
/// sides and optional label words stripped from the query, so "Makro-All-Shift" and "All Shift"
/// both resolve the camel-case macro "AllShift" — normalization drops hyphens without a
/// separator, which no token stage can bridge), fuzzy token cover (every word of the stored
/// name appears — possibly slightly damaged — in the query, which absorbs label words like
/// "Gruppe "/"Vertrag "), whole-string fuzzy equality, and finally Kölner Phonetik code
/// coverage for misheard spoken names ("Meier" vs "Mayer"). A token-covered match is only
/// auto-picked when every other covered candidate is a strict token subset of it; otherwise the
/// candidates are reported for disambiguation. It never silently picks one of several equally
/// plausible matches.
/// Under the default Strict policy only the stages that change no character of the query pick an
/// entity (normalized equality, a forward match on WHOLE words - "Nord" finds "Region Nord" and
/// "Nord-West" but not "Nordwest" -, compact equality after stripping label words that stand as
/// WHOLE leading or trailing words - "Gruppe Leitung" finds "Leitung", "Gruppenleitung" does not); a substring
/// that ends inside a word and every edit-distance, token or phonetic hit becomes a Suggested
/// outcome (one entity) or stays Ambiguous (several), so a write never runs on a name the user did
/// not say. Lenient runs every stage exactly as before and is meant for read-only call sites.
/// </summary>
/// <param name="labelWords">Optional type-label words ("makro", "macro") stripped from the
/// compact query as prefix or suffix, because spoken queries often decorate the entity name
/// with its type.</param>
/// <param name="policy">Strict (default, fail-closed) or Lenient (read-only call sites).</param>

namespace Klacks.Api.Application.Skills;

internal static class NameResolution
{
    public static NameResolutionResult<T> Resolve<T>(
        IReadOnlyList<T> items,
        Func<T, string> nameSelector,
        string? query,
        IReadOnlyList<string>? labelWords = null,
        NameMatchPolicy policy = NameMatchPolicy.Strict)
        where T : class
    {
        var (result, fromFuzzyStage) = ResolveStaged(items, nameSelector, query, labelWords, policy);
        return ApplyPolicy(result, fromFuzzyStage, policy);
    }

    // Multilingual entities (absence types, qualifications) carry one name per language plus
    // abbreviations: every non-empty variant competes in the staged matcher, and candidates
    // pointing at the same entity collapse back into a single match.
    public static NameResolutionResult<T> ResolveVariants<T>(
        IReadOnlyList<T> items,
        Func<T, IEnumerable<string?>> variantsSelector,
        string? query,
        IReadOnlyList<string>? labelWords = null,
        NameMatchPolicy policy = NameMatchPolicy.Strict)
        where T : class
    {
        var variants = items
            .SelectMany(item => variantsSelector(item)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => new NameVariant<T>(item, value!)))
            .ToList();

        var (resolution, fromFuzzyStage) = ResolveStaged(variants, variant => variant.Value, query, labelWords, policy);
        return ApplyPolicy(CollapseVariants(resolution), fromFuzzyStage, policy);
    }

    private static (NameResolutionResult<T> Result, bool FromFuzzyStage) ResolveStaged<T>(
        IReadOnlyList<T> items,
        Func<T, string> nameSelector,
        string? query,
        IReadOnlyList<string>? labelWords,
        NameMatchPolicy policy)
        where T : class
    {
        var normalizedQuery = NameMatching.Normalize(query);
        if (normalizedQuery.Length == 0)
        {
            return (NameResolutionResult<T>.NotFound(), false);
        }

        var named = items
            .Select(item => (Item: item, Normalized: NameMatching.Normalize(nameSelector(item))))
            .Where(x => x.Normalized.Length > 0)
            .ToList();

        var exact = named.Where(x => x.Normalized == normalizedQuery).ToList();
        if (exact.Count > 0)
        {
            return (FromStage(exact), false);
        }

        var forward = named
            .Where(x => x.Normalized.Contains(normalizedQuery, StringComparison.Ordinal))
            .ToList();
        var compactVariants = CompactQueryVariants(normalizedQuery, labelWords);
        if (policy == NameMatchPolicy.Lenient)
        {
            return forward.Count > 0
                ? (FromStage(forward), false)
                : (ResolveFuzzyStages(named, normalizedQuery, compactVariants), true);
        }

        var queryWords = NameMatching.WordTokens(query);
        var wholeWordForward = named
            .Where(x => ContainsWordSequence(NameMatching.WordTokens(nameSelector(x.Item)), queryWords))
            .ToList();
        if (wholeWordForward.Count > 0)
        {
            return (FromStage(wholeWordForward), false);
        }

        var strictCompactVariants = WholeWordCompactQueryVariants(query, labelWords);
        var compactExact = named
            .Where(x => strictCompactVariants.Contains(CompactOf(x.Normalized), StringComparer.Ordinal))
            .ToList();
        if (compactExact.Count > 0)
        {
            return (FromStage(compactExact), false);
        }

        return forward.Count > 0
            ? (FromStage(forward), true)
            : (ResolveFuzzyStages(named, normalizedQuery, compactVariants), true);
    }

    private static List<string> WholeWordCompactQueryVariants(string? query, IReadOnlyList<string>? labelWords)
    {
        var words = NameMatching.WordTokens(query);
        var variants = new List<string> { string.Concat(words) };
        if (labelWords == null)
        {
            return variants;
        }

        foreach (var labelWord in labelWords)
        {
            var label = NameMatching.WordTokens(labelWord);
            if (label.Length == 0 || words.Length <= label.Length)
            {
                continue;
            }

            if (words.Take(label.Length).SequenceEqual(label, StringComparer.Ordinal))
            {
                variants.Add(string.Concat(words.Skip(label.Length)));
            }

            if (words.Skip(words.Length - label.Length).SequenceEqual(label, StringComparer.Ordinal))
            {
                variants.Add(string.Concat(words.Take(words.Length - label.Length)));
            }
        }

        return variants.Distinct(StringComparer.Ordinal).ToList();
    }

    private static bool ContainsWordSequence(string[] nameWords, string[] queryWords)
    {
        if (queryWords.Length == 0 || queryWords.Length > nameWords.Length)
        {
            return false;
        }

        for (var start = 0; start <= nameWords.Length - queryWords.Length; start++)
        {
            var matches = true;
            for (var offset = 0; offset < queryWords.Length && matches; offset++)
            {
                matches = string.Equals(nameWords[start + offset], queryWords[offset], StringComparison.Ordinal);
            }

            if (matches)
            {
                return true;
            }
        }

        return false;
    }

    private static NameResolutionResult<T> ResolveFuzzyStages<T>(
        List<(T Item, string Normalized)> named,
        string normalizedQuery,
        List<string> compactVariants)
        where T : class
    {
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

        // Last resort for misheard spoken names ("Meier" vs "Mayer"): Kölner Phonetik code
        // coverage. Deliberately the weakest stage — the encoding ignores most vowels, so it
        // is over-inclusive ("Marie" ~ "Meier"); several phonetic hits surface as ambiguous
        // candidates instead of a silent pick.
        var queryCodes = NameMatching.PhoneticCodes(normalizedQuery);
        if (queryCodes.Length > 0)
        {
            var phonetic = named
                .Where(x => NameMatching.PhoneticTokensCovered(NameMatching.PhoneticCodes(x.Normalized), queryCodes))
                .ToList();
            if (phonetic.Count > 0)
            {
                return FromStage(phonetic);
            }
        }

        return NameResolutionResult<T>.NotFound();
    }

    private static NameResolutionResult<T> ApplyPolicy<T>(
        NameResolutionResult<T> result, bool fromFuzzyStage, NameMatchPolicy policy)
        where T : class
    {
        if (policy == NameMatchPolicy.Lenient || !fromFuzzyStage || result.Match == null)
        {
            return result;
        }

        return NameResolutionResult<T>.Suggested(result.Match);
    }

    private static NameResolutionResult<T> CollapseVariants<T>(NameResolutionResult<NameVariant<T>> resolution)
        where T : class
    {
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
