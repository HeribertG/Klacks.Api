// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Result of the NavigationTargetMatcher lookup.
/// </summary>
namespace Klacks.Api.Application.Klacksy.Models;

public sealed class NavigationMatchResult
{
    public required string? TargetId { get; init; }
    public required string? Route { get; init; }
    public required double Score { get; init; }
    public required NavigationMatchTier Tier { get; init; }
    public required IReadOnlyList<NavigationCandidate> Candidates { get; init; }

    /// <summary>
    /// Only an exact whole-phrase match resolving to exactly one allowed target counts as understood
    /// well enough to answer without the LLM ever seeing the request. Token-overlap and fuzzy hits, and
    /// exact hits shared by more than one target, are candidates for NavigationMissDetector only.
    /// </summary>
    public bool IsFastPath => Tier == NavigationMatchTier.Exact && Candidates.Count == 1;
}

public enum NavigationMatchTier
{
    None,
    Exact,
    TokenOverlap,
    Fuzzy,
}

public sealed record NavigationCandidate(string TargetId, string Route, double Score);
