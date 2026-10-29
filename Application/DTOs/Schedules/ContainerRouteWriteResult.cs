// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Outcome of optimizing and writing a container weekday route. On failure only Error is set and nothing was written.
/// </summary>
/// <param name="Error">Why nothing was written; null on success</param>
/// <param name="StartBase">Start base address the route was optimized from (empty when none could be resolved)</param>
/// <param name="EndBase">End base address the route was optimized to (empty when none could be resolved)</param>
/// <param name="TotalDistanceKm">Total route distance including the legs from and to the bases</param>
/// <param name="EstimatedTravelTime">Travel plus on-site time of the route (HH:mm)</param>
/// <param name="IsEstimated">True when travel times are straight-line estimates because the routing service was unreachable</param>
/// <param name="Stops">Planned visits in route order</param>
/// <param name="BreakCount">Number of break items kept in the template</param>

namespace Klacks.Api.Application.DTOs.Schedules;

public record ContainerRouteWriteResult(
    string? Error,
    string StartBase,
    string EndBase,
    double TotalDistanceKm,
    string EstimatedTravelTime,
    bool IsEstimated,
    IReadOnlyList<ContainerRouteStop> Stops,
    int BreakCount)
{
    public bool Success => Error == null;

    public static ContainerRouteWriteResult Failure(string error) =>
        new(error, string.Empty, string.Empty, 0, string.Empty, false, Array.Empty<ContainerRouteStop>(), 0);
}
