// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Services.RouteOptimization.ContainerAutofill;

/// <summary>
/// How a container autofill plan ended; only Routed carries a route.
/// </summary>
public enum ContainerAutofillPlanOutcome
{
    NoTimeRangeTasks = 0,
    NoLocatableTasks = 1,
    BaseNotLocated = 2,
    NothingSelected = 3,
    Routed = 4,
}
