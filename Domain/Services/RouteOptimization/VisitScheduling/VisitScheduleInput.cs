// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Everything the container visit scheduler reads.
/// </summary>
/// <param name="ContainerStartMinutes">Start of the container day (template FromTime) in minutes since midnight</param>
/// <param name="Route">Full optimized route including start and end base steps</param>
/// <param name="TravelTimeFromStartBase">Travel time used when the first route step is a visit</param>
/// <param name="TravelTimeToEndBase">Return travel written as travel after of the last visit</param>
/// <param name="DistanceToEndBaseKm">Return distance; the return travel is written only when it is above zero</param>
/// <param name="Visits">Visit items of the template</param>
/// <param name="Absences">Fixed breaks sorted by start (ContainerAbsenceTimeBlocks.BuildFixedIntervals)</param>

namespace Klacks.Api.Domain.Services.RouteOptimization.VisitScheduling;

public record VisitScheduleInput(
    int ContainerStartMinutes,
    IReadOnlyList<RouteStep> Route,
    TimeSpan TravelTimeFromStartBase,
    TimeSpan TravelTimeToEndBase,
    double DistanceToEndBaseKm,
    IReadOnlyList<VisitItem> Visits,
    IReadOnlyList<FixedTimeInterval> Absences);
