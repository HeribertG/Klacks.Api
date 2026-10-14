// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves the parameters of a bulk-delete sentence (employee by id or name, date range, optional shift) into the
/// exact Works it would delete. The confirmation preview and the confirmed skill call use this one planner, so what
/// the user is shown is what the call resolves again when it runs. Only reads; it refuses what must not be deleted
/// from chat (locked Works, sealed days, an unknown or hidden employee, nothing matching, more than the per-call cap).
/// </summary>

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

public interface IBulkWorkDeletionPlanner
{
    /// <param name="parameters">The skill call's parameters as the model supplied them</param>
    /// <param name="userLanguage">UI language of the caller, used to read written dates and day words</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<BulkWorkDeletionPlan> PlanAsync(
        Dictionary<string, object> parameters, string? userLanguage, CancellationToken cancellationToken = default);
}
