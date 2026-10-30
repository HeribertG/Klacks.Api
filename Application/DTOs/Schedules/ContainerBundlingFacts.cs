// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Services.RouteOptimization.ContainerAutofill;

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// What the dry run of a bundling found, before any judgement.
/// </summary>
/// <param name="Location">Base of the tour</param>
/// <param name="IsoWeekday">ISO weekday 1=Monday..7=Sunday</param>
/// <param name="FromTime">Start of the time budget</param>
/// <param name="UntilTime">End of the time budget</param>
/// <param name="ValidFrom">First day of the validity the services were read for</param>
/// <param name="ValidUntil">Last day of that validity; null = open ended</param>
/// <param name="TransportMode">Transport mode as asked</param>
/// <param name="GroupNames">Groups the services were limited to; empty = all the caller may see</param>
/// <param name="BreakMinutes">Length of the break to look for</param>
/// <param name="Outcome">How the plan ended</param>
/// <param name="TravelTimesEstimated">True when the plan rests on straight-line estimates</param>
/// <param name="CandidateCount">Mobile services that were looked at</param>
/// <param name="Route">The tour; null unless the outcome is Routed</param>
/// <param name="PlacementError">Why the bundling would refuse to place the visits; null when it would not</param>
/// <param name="LeftOut">All services that stay out, with the reason</param>
public sealed record ContainerBundlingFacts(
    string Location,
    int IsoWeekday,
    TimeOnly FromTime,
    TimeOnly UntilTime,
    DateOnly ValidFrom,
    DateOnly? ValidUntil,
    string TransportMode,
    IReadOnlyList<string> GroupNames,
    int BreakMinutes,
    ContainerAutofillPlanOutcome Outcome,
    bool TravelTimesEstimated,
    int CandidateCount,
    ContainerBundlingRouteFacts? Route,
    string? PlacementError,
    IReadOnlyList<ContainerBundlingLeftOut> LeftOut);
