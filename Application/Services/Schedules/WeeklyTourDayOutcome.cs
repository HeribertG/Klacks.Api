// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// What one weekday of one tour came to: how many services the tour has that day, how many the route placed, the distance, the
/// stops in route order and the customers whose service did not fit the time budget.
/// </summary>
/// <param name="IsoWeekday">ISO weekday 1=Monday..7=Sunday</param>
/// <param name="Visits">Number of services the tour has on that weekday</param>
/// <param name="Placed">Number of services placed in the template of that weekday</param>
/// <param name="DistanceKm">Total distance of the route</param>
/// <param name="TravelTimesEstimated">True when the travel times are straight-line estimates</param>
/// <param name="Stops">The placed stops in route order</param>
/// <param name="NotPlaced">Names of the customers whose service did not fit the time budget that day</param>

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

public sealed record WeeklyTourDayOutcome(
    int IsoWeekday,
    int Visits,
    int Placed,
    double DistanceKm,
    bool TravelTimesEstimated,
    IReadOnlyList<ContainerRouteStop> Stops,
    IReadOnlyList<string> NotPlaced);
