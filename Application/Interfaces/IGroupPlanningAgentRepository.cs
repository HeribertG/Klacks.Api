// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// Resolves the agents a planning run for a selected group works with — the same clients the schedule
/// view lists for that group and period.
/// </summary>
public interface IGroupPlanningAgentRepository
{
    /// <summary>
    /// Ids of the clients the schedule shows for the group and period: real (non-scenario) members of the
    /// group or any of its descendant groups, no customers, membership overlapping the period, intersected
    /// with the caller's group visibility (unrestricted for background callers without a user).
    /// </summary>
    /// <param name="groupId">Selected group; may be a root, an inner node or a leaf</param>
    /// <param name="periodFrom">First day of the planning period, inclusive</param>
    /// <param name="periodUntil">Last day of the planning period, inclusive</param>
    /// <returns>Distinct client ids; empty for an unknown group or a group without members</returns>
    Task<List<Guid>> GetAgentIdsAsync(
        Guid groupId, DateOnly periodFrom, DateOnly periodUntil, CancellationToken cancellationToken = default);

    /// <summary>
    /// The same clients as <see cref="GetAgentIdsAsync"/> (same scope, group filter and visibility), each with the
    /// employment window the schedule locks days by, read in one query without tracking. Used to count, for a group, who
    /// the grid shows dark.
    /// </summary>
    /// <param name="groupId">Selected group; may be a root, an inner node or a leaf</param>
    /// <param name="periodFrom">First day of the checked period, inclusive</param>
    /// <param name="periodUntil">Last day of the checked period, inclusive</param>
    /// <returns>One row per client; empty for an unknown group or a group without members</returns>
    Task<List<PlanningAgentEmployment>> GetAgentEmploymentsAsync(
        Guid groupId, DateOnly periodFrom, DateOnly periodUntil, CancellationToken cancellationToken = default);
}
