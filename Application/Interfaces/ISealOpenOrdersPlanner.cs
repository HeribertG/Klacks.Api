// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Orders;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// Resolves a seal_open_orders call (group scope, group name, dates, filter checks) into its dry-run, the one resolution
/// the skill and its confirmation preview provider share, so what the user confirms and what the confirmed replay
/// compares against can never be computed two different ways. Only reads; the caller's permission is checked by the skill.
/// </summary>
public interface ISealOpenOrdersPlanner
{
    Task<SealOpenOrdersPlan> PlanAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default);
}
