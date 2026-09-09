// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Score thresholds shared by navigation matching, fast-path routing and suspected-miss detection.
/// Kept in one place so the matcher and the code that reads its scores can never drift apart.
/// </summary>
namespace Klacks.Api.Application.Klacksy.Models;

public static class NavigationMatchThresholds
{
    public const double FastPath = 0.85;
    public const double MinScoreForMatch = 0.5;
}
