// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Klacksy;

using Klacks.Api.Application.Klacksy.Models;

/// <summary>
/// Tier1 matcher. Exact synonym match → score 1.0. Token-overlap → score 0.5–0.85.
/// Filters out targets whose RequiredPermission is not held by the user.
/// </summary>
public sealed class NavigationTargetMatcher : INavigationTargetMatcher
{
    private const int MaxCandidates = 3;
    private const double MaxTokenOverlapScore = 0.85;
    private const double MinScoreForMatch = 0.5;

    private readonly INavigationTargetCacheService _cache;

    public NavigationTargetMatcher(INavigationTargetCacheService cache) => _cache = cache;

    public NavigationMatchResult Match(string normalizedUtterance, string locale, IReadOnlyCollection<string> userPermissions)
    {
        if (string.IsNullOrWhiteSpace(normalizedUtterance))
            return Empty();

        var exact = _cache.FindBySynonym(normalizedUtterance, locale)
            .Where(t => IsAllowed(t, userPermissions))
            .FirstOrDefault();

        if (exact != null)
            return new NavigationMatchResult
            {
                TargetId = exact.TargetId,
                Route = exact.Route,
                Score = 1.0,
                Candidates = new[] { new NavigationCandidate(exact.TargetId, exact.Route, 1.0) }
            };

        var tokens = normalizedUtterance.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var scored = new Dictionary<string, (NavigationTarget Target, double Score)>();
        foreach (var token in tokens)
        {
            foreach (var hit in _cache.FindBySynonym(token, locale).Where(t => IsAllowed(t, userPermissions)))
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
            TargetId = top?.Score > MinScoreForMatch ? top.TargetId : null,
            Route = top?.Score > MinScoreForMatch ? top.Route : null,
            Score = top?.Score ?? 0,
            Candidates = candidates
        };
    }

    private static bool IsAllowed(NavigationTarget t, IReadOnlyCollection<string> perms)
        => string.IsNullOrEmpty(t.RequiredPermission) || perms.Contains(t.RequiredPermission);

    private static NavigationMatchResult Empty() => new()
    {
        TargetId = null, Route = null, Score = 0, Candidates = Array.Empty<NavigationCandidate>()
    };
}
