// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves the ordered call list for a group's escalation chain: assigned planners first (by
/// DerivedRank, an admin OverrideRank always wins), then the global admin role as a last-resort
/// stage (Owner decision A2, docs/ENTWURF-eskalationskette-2026-08-16.md §3). Also re-derives
/// EscalationRosterEntry.DerivedRank from GroupVisibility on demand.
/// </summary>
/// <param name="groupId">Any group id in the target group's subtree; resolved to its root before lookup.</param>

using Klacks.Api.Domain.Models.Assistant.Escalation;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IEscalationRosterService
{
    Task<IReadOnlyList<EscalationRosterCandidate>> GetOrderedRosterAsync(Guid groupId, CancellationToken cancellationToken = default);
}
