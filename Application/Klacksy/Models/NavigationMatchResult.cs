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
    public required IReadOnlyList<NavigationCandidate> Candidates { get; init; }
    public bool IsFastPath => Score > 0.85;
}

public sealed record NavigationCandidate(string TargetId, string Route, double Score);
