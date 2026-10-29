// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// One stop of an optimized route as the route endpoints return it: the location, the shift it serves (Guid.Empty for
/// a start or end base) and the leg to the next stop.
/// </summary>
/// <param name="Order">1-based position on the route</param>
/// <param name="TravelTimeToNext">Travel time to the next stop; zero on the last stop</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Services.RouteOptimization.VisitScheduling;

public record RouteStep(
    int Order,
    string Name,
    string Address,
    double Latitude,
    double Longitude,
    Guid ShiftId,
    TransportMode TransportMode,
    double DistanceToNextKm,
    TimeSpan TravelTimeToNext);
