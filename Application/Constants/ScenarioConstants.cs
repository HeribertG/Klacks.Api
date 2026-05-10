// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Constants;

/// <summary>
/// Shared scenario-mode constants. Single source of truth for both the
/// AnalyseScenario clone scope and the AutoWizard validator context window.
/// </summary>
public static class ScenarioConstants
{
    /// <summary>
    /// Number of days extending the period on each side for boundary context.
    /// Used by both clone operations (so cross-period shifts/works are visible
    /// in scenario mode) and wizard validators (so MaxConsecutive / MinRest
    /// runs crossing the period boundary are detected).
    /// </summary>
    public const int BoundaryDays = 14;
}
