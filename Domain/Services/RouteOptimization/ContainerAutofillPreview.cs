// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// What a container autofill would do, without anything stored.
/// </summary>
/// <param name="Outcome">How the plan ended</param>
/// <param name="Result">The route result; the empty result unless the outcome is Routed</param>
/// <param name="TravelTimesEstimated">True when the plan rests on straight-line estimates</param>
/// <param name="TimeRangeTaskCount">Number of TimeRange tasks that were looked at</param>
/// <param name="Unlocated">Tasks without a route candidate and why</param>
/// <param name="Omissions">Located tasks the route leaves out and why</param>

using Klacks.Api.Domain.Services.RouteOptimization.ContainerAutofill;

namespace Klacks.Api.Domain.Services.RouteOptimization;

public record ContainerAutofillPreview(
    ContainerAutofillPlanOutcome Outcome,
    ContainerAutofillResult Result,
    bool TravelTimesEstimated,
    int TimeRangeTaskCount,
    IReadOnlyList<ContainerAutofillUnlocatedTask> Unlocated,
    IReadOnlyList<ContainerAutofillOmission> Omissions);
