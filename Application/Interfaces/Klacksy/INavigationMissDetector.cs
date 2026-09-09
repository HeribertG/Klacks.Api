// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Interfaces.Klacksy;

using Klacks.Api.Application.Klacksy.Models;

/// <summary>
/// Detects a navigation the model performed without an explicit in-page target, when the synonym
/// matcher's candidates for this turn suggest the user meant a specific spot on the page the model
/// actually landed on.
/// </summary>
public interface INavigationMissDetector
{
    NavigationCandidate? DetectSuspectedMiss(NavigationMatchResult match, string? navigatedRoute, string? navigatedTarget);
}
