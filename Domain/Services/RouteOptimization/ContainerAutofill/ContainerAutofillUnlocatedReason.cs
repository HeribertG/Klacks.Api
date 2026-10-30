// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Services.RouteOptimization.ContainerAutofill;

/// <summary>
/// Why a task did not become a route candidate.
/// </summary>
public enum ContainerAutofillUnlocatedReason
{
    NoAddress = 0,
    SameAddress = 1,
    NotLocated = 2,
    NotLookedUp = 3,
}
