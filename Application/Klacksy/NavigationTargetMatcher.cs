// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Klacksy;

using Klacks.Api.Application.Interfaces.Klacksy;
using Klacks.Api.Application.Klacksy.Models;

/// <summary>
/// Three-tier matcher used by the chat fast-path:
///   Tier 1 — exact synonym (locale, then any-locale) → score 1.0
///   Tier 2 — token-overlap across synonyms → score 0.5–0.85
///   Tier 3 — trigram-Jaccard fuzzy match against all synonyms → score ≤ 0.85
/// Targets whose RequiredPermission the user lacks are filtered out at every tier.
/// </summary>
public sealed class NavigationTargetMatcher : INavigationTargetMatcher
{
    private const int MaxCandidates = 3;
    private const double MaxTokenOverlapScore = 0.85;
    private const double MinScoreForMatch = 0.5;
    private const double FuzzyMinScore = 0.6;
    private const int TrigramSize = 3;

    private readonly INavigationTargetCacheService _cache;

    public NavigationTargetMatcher(INavigationTargetCacheService cache) => _cache = cache;

    public NavigationMatchResult Match(string normalizedUtterance, string locale, IReadOnlyCollection<string> userPermissions)
    {
        if (string.IsNullOrWhiteSpace(normalizedUtterance))
            return Empty();

        var exact = FindFirstAllowed(_cache.FindBySynonym(normalizedUtterance, locale), userPermissions)
                    ?? FindFirstAllowed(_cache.FindBySynonymAnyLocale(normalizedUtterance), userPermissions);

        if (exact != null)
            return new NavigationMatchResult
            {
                TargetId = exact.TargetId,
                Route = exact.Route,
                Score = 1.0,
                Candidates = new[] { new NavigationCandidate(exact.TargetId, exact.Route, 1.0) }
            };

        var tokenResult = TokenOverlap(normalizedUtterance, locale, userPermissions);
        if (tokenResult.Score >= MinScoreForMatch)
            return tokenResult;

        var fuzzyResult = Fuzzy(normalizedUtterance, locale, userPermissions);
        if (fuzzyResult.Score >= FuzzyMinScore)
            return fuzzyResult;

        return tokenResult.Candidates.Count > 0 ? tokenResult : fuzzyResult;
    }

    private NavigationMatchResult TokenOverlap(string utterance, string locale, IReadOnlyCollection<string> userPermissions)
    {
        var tokens = utterance.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var scored = new Dictionary<string, (NavigationTarget Target, double Score)>();
        foreach (var token in tokens)
        {
            var hits = _cache.FindBySynonym(token, locale);
            if (hits.Count == 0)
                hits = _cache.FindBySynonymAnyLocale(token);

            foreach (var hit in hits.Where(t => IsAllowed(t, userPermissions)))
            {
                if (scored.TryGetValue(hit.TargetId, out var current))
                    scored[hit.TargetId] = (current.Target, current.Score + 1.0 / tokens.Length);
                else
                    scored[hit.TargetId] = (hit, 1.0 / tokens.Length);
            }
        }

        var candidates = scored.Values
            .Select(v => new NavigationCandidate(v.Target.TargetId, v.Target.Route, Math.Min(v.Score, MaxTokenOverlapScore)))
            .OrderByDescending(c => c.Score)
            .Take(MaxCandidates)
            .ToList();

        var top = candidates.FirstOrDefault();
        return new NavigationMatchResult
        {
            TargetId = top?.Score >= MinScoreForMatch ? top.TargetId : null,
            Route = top?.Score >= MinScoreForMatch ? top.Route : null,
            Score = top?.Score ?? 0,
            Candidates = candidates
        };
    }

    private NavigationMatchResult Fuzzy(string utterance, string locale, IReadOnlyCollection<string> userPermissions)
    {
        var inputTrigrams = Trigrams(utterance);
        if (inputTrigrams.Count == 0)
            return Empty();

        var all = _cache.All;
        if (all == null || all.Count == 0)
            return Empty();

        var scored = new List<(NavigationTarget Target, double Score)>();
        foreach (var target in all)
        {
            if (!IsAllowed(target, userPermissions))
                continue;

            var maxJaccard = 0.0;
            foreach (var synonym in SynonymsForLocale(target, locale))
            {
                var jaccard = Jaccard(inputTrigrams, Trigrams(synonym));
                if (jaccard > maxJaccard) maxJaccard = jaccard;
                if (maxJaccard >= 1.0) break;
            }

            if (maxJaccard >= FuzzyMinScore)
                scored.Add((target, Math.Min(maxJaccard, MaxTokenOverlapScore)));
        }

        var candidates = scored
            .OrderByDescending(x => x.Score)
            .Take(MaxCandidates)
            .Select(x => new NavigationCandidate(x.Target.TargetId, x.Target.Route, x.Score))
            .ToList();

        var top = candidates.FirstOrDefault();
        return new NavigationMatchResult
        {
            TargetId = top?.Score >= FuzzyMinScore ? top.TargetId : null,
            Route = top?.Score >= FuzzyMinScore ? top.Route : null,
            Score = top?.Score ?? 0,
            Candidates = candidates
        };
    }

    private static IEnumerable<string> SynonymsForLocale(NavigationTarget target, string locale)
    {
        if (target.Synonyms.TryGetValue(locale, out var primary))
        {
            foreach (var s in primary) yield return s;
        }

        if (locale != "en" && target.Synonyms.TryGetValue("en", out var english))
        {
            foreach (var s in english) yield return s;
        }
    }

    private static HashSet<string> Trigrams(string text)
    {
        var set = new HashSet<string>();
        var normalized = "  " + text.Trim().ToLowerInvariant() + "  ";
        if (normalized.Length < TrigramSize) return set;
        for (var i = 0; i <= normalized.Length - TrigramSize; i++)
            set.Add(normalized.Substring(i, TrigramSize));
        return set;
    }

    private static double Jaccard(HashSet<string> a, HashSet<string> b)
    {
        if (a.Count == 0 || b.Count == 0) return 0;
        var intersect = 0;
        var smaller = a.Count <= b.Count ? a : b;
        var larger = a.Count <= b.Count ? b : a;
        foreach (var item in smaller)
        {
            if (larger.Contains(item)) intersect++;
        }
        var union = a.Count + b.Count - intersect;
        return union == 0 ? 0 : (double)intersect / union;
    }

    private static NavigationTarget? FindFirstAllowed(IReadOnlyList<NavigationTarget> targets, IReadOnlyCollection<string> userPermissions)
    {
        foreach (var target in targets)
        {
            if (IsAllowed(target, userPermissions))
                return target;
        }

        return null;
    }

    private static bool IsAllowed(NavigationTarget t, IReadOnlyCollection<string> perms)
        => string.IsNullOrEmpty(t.RequiredPermission) || perms.Contains(t.RequiredPermission);

    private static NavigationMatchResult Empty() => new()
    {
        TargetId = null, Route = null, Score = 0, Candidates = Array.Empty<NavigationCandidate>()
    };
}
