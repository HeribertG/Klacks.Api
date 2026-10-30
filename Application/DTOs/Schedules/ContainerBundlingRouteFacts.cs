// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// The planned tour as figures: time and distance only.
/// </summary>
/// <param name="Stops">Placed visits in route order; empty when they cannot be placed</param>
/// <param name="VisitCount">Number of selected visits</param>
/// <param name="TravelMinutes">Travel from and back to the base, with the configured minimum travel time per leg</param>
/// <param name="OnSiteMinutes">Briefing, work and debriefing of all visits</param>
/// <param name="RemainingMinutes">What is left of the time budget</param>
/// <param name="DistanceKm">Length of the tour</param>
public sealed record ContainerBundlingRouteFacts(
    IReadOnlyList<ContainerRouteStop> Stops,
    int VisitCount,
    int TravelMinutes,
    int OnSiteMinutes,
    int RemainingMinutes,
    double DistanceKm);
