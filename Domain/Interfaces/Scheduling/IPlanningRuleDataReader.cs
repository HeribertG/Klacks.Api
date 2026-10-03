// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Read access the planning-rule loader needs besides the rule rows: group memberships for Group scopes and
/// the persisted Work rows outside the planning period (carry-in and neighbour days).
/// </summary>

using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Domain.Interfaces.Scheduling;

public interface IPlanningRuleDataReader
{
    /// <summary>
    /// Client memberships of the given groups (no subgroup expansion here) restricted to
    /// <paramref name="clientIds"/>, whose validity overlaps [from, until]. Real memberships always count; in a
    /// scenario (<paramref name="analyseToken"/> set) the scenario's own memberships count as well.
    /// </summary>
    Task<List<PlanningRuleGroupMembership>> GetGroupMembershipsAsync(
        IReadOnlyCollection<Guid> groupIds,
        IReadOnlyCollection<Guid> clientIds,
        DateOnly from,
        DateOnly until,
        Guid? analyseToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Not deleted top-level Work rows (ParentWorkId null - container sub-works are part of their container, not
    /// shifts of their own) of <paramref name="clientIds"/> dated in [from, until] whose AnalyseToken equals
    /// <paramref name="analyseToken"/> (null = real plan), the filter of PreCommitConflictChecker and the timeline.
    /// </summary>
    Task<List<PlanningRuleWorkSpan>> GetWorkSpansAsync(
        IReadOnlyCollection<Guid> clientIds,
        DateOnly from,
        DateOnly until,
        Guid? analyseToken,
        CancellationToken cancellationToken = default);
}
