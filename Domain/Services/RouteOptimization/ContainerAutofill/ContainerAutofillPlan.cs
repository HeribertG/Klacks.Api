// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Services.RouteOptimization.ContainerAutofill;

/// <summary>
/// Result of the planning stage.
/// </summary>
/// <param name="Outcome">How the plan ended</param>
/// <param name="Result">The route result, or the empty result when the outcome is not Routed</param>
/// <param name="TravelTimesEstimated">True when the plan rests on straight-line estimates, also when it selected nothing</param>
/// <param name="Context">Matrix and limits the route was evaluated against; null when no matrix was built</param>
/// <param name="Route">Matrix indices of the planned stops; empty when nothing was selected</param>
public sealed record ContainerAutofillPlan(
    ContainerAutofillPlanOutcome Outcome,
    ContainerAutofillResult Result,
    bool TravelTimesEstimated,
    ContainerAutofillRouteContext? Context,
    List<int> Route);
