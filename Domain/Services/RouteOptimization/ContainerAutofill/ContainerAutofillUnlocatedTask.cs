// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Services.RouteOptimization.ContainerAutofill;

/// <summary>
/// A task without a route candidate.
/// </summary>
/// <param name="ShiftId">The task</param>
/// <param name="Reason">Why it has no candidate</param>
public sealed record ContainerAutofillUnlocatedTask(Guid ShiftId, ContainerAutofillUnlocatedReason Reason);
