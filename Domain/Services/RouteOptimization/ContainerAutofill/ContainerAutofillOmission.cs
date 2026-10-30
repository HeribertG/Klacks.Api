// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Services.RouteOptimization.ContainerAutofill;

/// <summary>
/// A located task the planned tour leaves out.
/// </summary>
/// <param name="ShiftId">The task</param>
/// <param name="Reason">Why it is left out</param>
public sealed record ContainerAutofillOmission(Guid ShiftId, ContainerAutofillOmissionReason Reason);
