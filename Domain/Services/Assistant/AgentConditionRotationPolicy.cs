// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Picks which candidates of a ledger-tracked detector are reported in one tick, so that every
/// candidate is offered again eventually instead of a fixed head of the list being reported forever.
/// A detector that simply orders by a business key and caps starves everything behind the cap whenever
/// that key is constant across the backlog - the normal shape of bulk-created data - and starvation
/// here is silent: the planner notification, the open-findings skill, the digest and the action
/// dispatcher all read the ledger, which only ever learns what a tick emitted.
///
/// The rule is a two-group rotation: candidates the ledger has never opened a row for come first (they
/// have no observation to be behind), and behind them the already-opened ones least recently observed
/// first. Because the caller advances LastSeenAtUtc on everything it reports, a reported candidate
/// moves to the back of the queue and the whole backlog cycles in ceil(candidates / cap) ticks.
/// </summary>
/// <remarks>
/// A candidate whose ledger row already reached a terminal status carries no entry in
/// <c>lastSeenByEntityId</c> either (the caller passes the OPEN rows), so it sorts with the never-opened
/// group. That is intended: a terminal row is not being tracked any more, and re-reporting it is what
/// opens a fresh one.
/// </remarks>

namespace Klacks.Api.Domain.Services.Assistant;

public static class AgentConditionRotationPolicy
{
    /// <summary>
    /// The rotated, capped selection - deterministic, side-effect free and independent of any store.
    /// </summary>
    /// <param name="candidates">Every candidate the detector's predicates matched, uncapped.</param>
    /// <param name="idOf">Reads the entity id a ledger row would carry for a candidate.</param>
    /// <param name="lastSeenByEntityId">LastSeenAtUtc of the ledger rows still OPEN for this kind, by entity id. A candidate missing here counts as never opened.</param>
    /// <param name="primaryOrder">The detector's own business order, used inside both groups and as the tiebreaker between equal observation times.</param>
    /// <param name="cap">Maximum number of candidates to report in this tick.</param>
    public static IReadOnlyList<TCandidate> Select<TCandidate>(
        IReadOnlyList<TCandidate> candidates,
        Func<TCandidate, Guid> idOf,
        IReadOnlyDictionary<Guid, DateTime> lastSeenByEntityId,
        Comparison<TCandidate> primaryOrder,
        int cap)
    {
        if (candidates.Count == 0 || cap <= 0)
        {
            return Array.Empty<TCandidate>();
        }

        var businessOrder = Comparer<TCandidate>.Create(primaryOrder);

        return candidates
            .OrderBy(candidate => lastSeenByEntityId.ContainsKey(idOf(candidate)))
            .ThenBy(candidate => LastSeenOf(candidate, idOf, lastSeenByEntityId))
            .ThenBy(candidate => candidate, businessOrder)
            .Take(cap)
            .ToList();
    }

    private static DateTime LastSeenOf<TCandidate>(
        TCandidate candidate,
        Func<TCandidate, Guid> idOf,
        IReadOnlyDictionary<Guid, DateTime> lastSeenByEntityId) =>
        lastSeenByEntityId.TryGetValue(idOf(candidate), out var lastSeenAtUtc) ? lastSeenAtUtc : DateTime.MinValue;
}
