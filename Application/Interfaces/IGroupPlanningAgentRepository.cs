// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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
}
