// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Klacksy;

using Klacks.Api.Application.Interfaces.Klacksy;
using Klacks.Api.Application.Klacksy.Models;

/// <summary>
/// Flags a navigation the model performed without an in-page target although the synonym matcher saw a
/// plausible in-page candidate on that very route. Below the fast-path threshold the match was never
/// acted on; above the minimum it is too strong to ignore once the model lands on the same page.
/// Page-level catalog entries (Category == "page") are excluded: they stand for the page itself and
/// have no data-klacksy-target marker to scroll to, so sharing the route is not a miss.
/// </summary>
/// <param name="targetCache">Resolves a candidate's Category to exclude page-level entries</param>
public sealed class NavigationMissDetector : INavigationMissDetector
{
    private readonly INavigationTargetCacheService _targetCache;

    public NavigationMissDetector(INavigationTargetCacheService targetCache) => _targetCache = targetCache;

    public NavigationCandidate? DetectSuspectedMiss(NavigationMatchResult match, string? navigatedRoute, string? navigatedTarget)
    {
        if (string.IsNullOrEmpty(navigatedRoute) || !string.IsNullOrEmpty(navigatedTarget))
        {
            return null;
        }

        return match.Candidates
            .Where(c => c.Score >= NavigationMatchThresholds.MinScoreForMatch
                     && c.Score < NavigationMatchThresholds.FastPath
                     && RouteMatches(c.Route, navigatedRoute)
                     && !IsPageLevel(c.TargetId))
            .OrderByDescending(c => c.Score)
            .FirstOrDefault();
    }

    private bool IsPageLevel(string targetId)
        => _targetCache.GetById(targetId)?.Category == NavigationTargetCategories.PageLevel;

    private static bool RouteMatches(string candidateRoute, string navigatedRoute)
    {
        var route = StripQuery(navigatedRoute);
        return route == candidateRoute
            || route.StartsWith(candidateRoute + "/", StringComparison.Ordinal);
    }

    private static string StripQuery(string route)
    {
        var index = route.IndexOf('?');
        return index >= 0 ? route[..index] : route;
    }
}
