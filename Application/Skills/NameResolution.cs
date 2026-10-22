// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Shared staged by-name resolution used by the entity resolvers (group, contract, macro,
/// absence type, qualification). Lenient stages, in order: exact normalized equality, forward
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
/// The default Strict policy picks an entity only on: equal words (case- and punctuation-insensitive;
/// diacritic-insensitive only when the query carries no diacritics, otherwise they must match exactly,
/// so "Zurich" finds "Zürich" but "Bác" never finds "Bắc"), a forward match on WHOLE words ("Nord" finds
/// "Region Nord" and "Nord-West", not "Nordwest"), a substring for scripts written without word
/// separators (CJK, Thai, Lao, Khmer, Myanmar: "東京" finds "東京支店"), and compact equality after
/// stripping label words that stand as whole leading or trailing words ("Gruppe Leitung" finds
/// "Leitung", "Gruppenleitung" does not). A query that is nothing but a label word finds nothing. Every
/// other hit — a substring ending inside a word, a diacritic mismatch, an edit-distance, token or
/// phonetic match — is the Lenient result downgraded to Suggested (one entity) or left Ambiguous
/// (several), so a write never runs on a name the user did not say. Lenient is meant for read-only call
/// sites and runs exactly the previous stages.
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
        if (policy == NameMatchPolicy.Lenient)
        {
            return ResolveLenient(items, nameSelector, query, labelWords);
        }

        var (result, fromFuzzyStage) = ResolveStrict(items, nameSelector, query, labelWords);
        return fromFuzzyStage ? Downgrade(result) : result;
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

        if (policy == NameMatchPolicy.Lenient)
        {
            return CollapseVariants(ResolveLenient(variants, variant => variant.Value, query, labelWords));
        }

        var (result, fromFuzzyStage) = ResolveStrict(variants, variant => variant.Value, query, labelWords);
        var collapsed = CollapseVariants(result);
        return fromFuzzyStage ? Downgrade(collapsed) : collapsed;
    }

    private static (NameResolutionResult<T> Result, bool FromFuzzyStage) ResolveStrict<T>(
        IReadOnlyList<T> items,
        Func<T, string> nameSelector,
        string? query,
        IReadOnlyList<string>? labelWords)
        where T : class
    {
        var keepDiacritics = NameMatching.HasDiacritics(query);
        var queryWords = NameMatching.WordTokens(query, keepDiacritics);
        if (queryWords.Length == 0 || IsOnlyLabelWords(queryWords, labelWords, keepDiacritics))
        {
            return (NameResolutionResult<T>.NotFound(), false);
        }

        var named = items
            .Select(item => (Item: item, Words: NameMatching.WordTokens(nameSelector(item), keepDiacritics)))
            .Where(x => x.Words.Length > 0)
            .ToList();

        var exact = named.Where(x => x.Words.SequenceEqual(queryWords, StringComparer.Ordinal)).ToList();
        if (exact.Count > 0)
        {
            return (FromMatches(exact.Select(x => x.Item).ToList()), false);
        }

        var wholeWordForward = named.Where(x => ContainsWordSequence(x.Words, queryWords)).ToList();
        if (wholeWordForward.Count > 0)
        {
            return (FromMatches(wholeWordForward.Select(x => x.Item).ToList()), false);
        }

        var joinedQuery = string.Join(' ', queryWords);
        if (NameMatching.IsUnsegmentedScript(joinedQuery))
        {
            var substring = named
                .Where(x => string.Join(' ', x.Words).Contains(joinedQuery, StringComparison.Ordinal))
                .ToList();
            if (substring.Count > 0)
            {
                return (FromMatches(substring.Select(x => x.Item).ToList()), false);
            }
        }

        var compactVariants = WholeWordCompactQueryVariants(queryWords, labelWords, keepDiacritics);
        var compactExact = named
            .Where(x => compactVariants.Contains(string.Concat(x.Words), StringComparer.Ordinal))
            .ToList();
        if (compactExact.Count > 0)
        {
            return (FromMatches(compactExact.Select(x => x.Item).ToList()), false);
        }

        return (ResolveLenient(items, nameSelector, query, labelWords), true);
    }

    private static NameResolutionResult<T> ResolveLenient<T>(
        IReadOnlyList<T> items,
        Func<T, string> nameSelector,
        string? query,
        IReadOnlyList<string>? labelWords)
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

    private static NameResolutionResult<T> Downgrade<T>(NameResolutionResult<T> result)
        where T : class
    {
        return result.Match == null ? result : NameResolutionResult<T>.Suggested(result.Match);
    }

    private static bool IsOnlyLabelWords(string[] queryWords, IReadOnlyList<string>? labelWords, bool keepDiacritics)
    {
        return labelWords != null
               && labelWords.Any(label =>
                   NameMatching.WordTokens(label, keepDiacritics).SequenceEqual(queryWords, StringComparer.Ordinal));
    }

    private static List<string> WholeWordCompactQueryVariants(
        string[] queryWords, IReadOnlyList<string>? labelWords, bool keepDiacritics)
    {
        var variants = new List<string> { string.Concat(queryWords) };
        if (labelWords == null)
        {
            return variants;
        }

        foreach (var labelWord in labelWords)
        {
            var label = NameMatching.WordTokens(labelWord, keepDiacritics);
            if (label.Length == 0 || queryWords.Length <= label.Length)
            {
                continue;
            }

            if (queryWords.Take(label.Length).SequenceEqual(label, StringComparer.Ordinal))
            {
                variants.Add(string.Concat(queryWords.Skip(label.Length)));
            }

            if (queryWords.Skip(queryWords.Length - label.Length).SequenceEqual(label, StringComparer.Ordinal))
            {
                variants.Add(string.Concat(queryWords.Take(queryWords.Length - label.Length)));
            }
        }

        return variants.Distinct(StringComparer.Ordinal).ToList();
    }

    private static bool ContainsWordSequence(string[] nameWords, string[] queryWords)
    {
        if (queryWords.Length > nameWords.Length)
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

    private static NameResolutionResult<T> FromMatches<T>(List<T> matches)
        where T : class
    {
        return matches.Count == 1
            ? NameResolutionResult<T>.Single(matches[0])
            : NameResolutionResult<T>.Ambiguous(matches);
    }

    private static NameResolutionResult<T> FromStage<T>(List<(T Item, string Normalized)> matches)
        where T : class
    {
        return FromMatches(matches.Select(m => m.Item).ToList());
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
