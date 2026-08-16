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

    /// <summary>Admin view of the raw roster rows (including orphaned ones) for the reorder UI - the
    /// global admin fallback stage (A2) is deliberately not included, it is a fixed rule, not a row.</summary>
    Task<IReadOnlyList<EscalationRosterEntryDetail>> GetRosterEntriesAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>Sets OverrideRank to the 1-based position of each user id in orderedUserIds. A user id
    /// with no current roster row for this group is ignored (stale client-side list).</summary>
    Task SetOrderAsync(Guid groupId, IReadOnlyList<string> orderedUserIds, CancellationToken cancellationToken = default);
}
