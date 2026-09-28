// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The goldset half of the description gate, in two phases so a proposal is judged on a pair of replays of
/// the same items in the same run: planned once, replayed with the current description, replayed again after
/// the proposal was applied.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IGoldsetHoldoutReplayGate
{
    /// <summary>
    /// Chooses the items a proposal for the named skill is judged on: the holdout items of the latest full run
    /// of the default goldset that involve the skill, and the given train misses that resolve in a learning
    /// goldset. Null when nothing can be measured YET - no full run, or the default goldset unreadable; that
    /// can change with the next run. A plan with no items at all means this proposal has nothing to replay
    /// against the current reference run - a permanent condition the caller must close, not keep pending.
    /// </summary>
    Task<GoldsetReplayPlan?> PlanAsync(
        string skillName, IReadOnlyList<GoldsetItemRef> trainMisses, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replays every planned item against the CURRENTLY LIVE catalogue. True when the expected tool (for a
    /// no-tool item: no tool) was chosen, false when anything else was, null when the provider did not answer,
    /// the replay threw or a recipe took the turn.
    /// </summary>
    Task<IReadOnlyDictionary<GoldsetItemRef, bool?>> ReplayAsync(
        GoldsetReplayPlan plan, CancellationToken cancellationToken = default);
}
