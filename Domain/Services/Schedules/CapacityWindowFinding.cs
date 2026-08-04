// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Result of evaluating one time window against the capacity reserve rule.
/// </summary>
/// <param name="Kind">Which window this is: single day, rolling three days, work week or calendar week</param>
/// <param name="From">First calendar day of the window</param>
/// <param name="Until">Last calendar day of the window</param>
/// <param name="Demand">Sum of scheduled shifts across the window</param>
/// <param name="Available">Sum of desired readiness minus existing and requested absences across the window</param>
/// <param name="Utilization">Demand divided by Available; null when no capacity is left at all</param>
/// <param name="NoCapacityLeft">True when nothing is available while shifts still need staffing</param>

namespace Klacks.Api.Domain.Services.Schedules;

public sealed record CapacityWindowFinding(
    CapacityWindowKind Kind,
    DateOnly From,
    DateOnly Until,
    double Demand,
    double Available,
    double? Utilization,
    bool NoCapacityLeft);
