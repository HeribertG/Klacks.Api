// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for GoalCandidate entities (self-directed goals, Phase 1 shadow mode / Phase 2 inbox).
/// Persists reflection-generated goal intents, supports the dedup lookup used before a new candidate
/// is written, and lists a user's own candidates for the read-only inbox.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IGoalCandidateRepository
{
    Task<GoalCandidate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(GoalCandidate candidate, CancellationToken cancellationToken = default);

    Task UpdateAsync(GoalCandidate candidate, CancellationToken cancellationToken = default);

    Task<bool> ExistsRecentAsync(string userId, string dedupHash, DateTime sinceUtc, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GoalCandidate>> GetRecentAsync(string userId, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists a user's own candidates, newest first. When <paramref name="status"/> is null, only
    /// non-terminal candidates (Shadow/Proposed) are returned; otherwise only candidates matching the
    /// given status. When <paramref name="take"/> is null or non-positive, all matching rows are returned.
    /// </summary>
    Task<IReadOnlyList<GoalCandidate>> GetForUserAsync(string userId, string? status, int? take, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the candidate a plan was drafted from. Used to verify that a self-reflection plan really
    /// traces back to a human approval before it is granted an elevated autonomy level.
    /// </summary>
    Task<GoalCandidate?> GetByPlanIdAsync(Guid planId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists approved candidates that already have a drafted plan, oldest first, limited by
    /// <paramref name="limit"/>. Feeds the Phase 4 retry sweep
    /// (GoalPlanExecutionRetryBackgroundService) that re-attempts execution for a candidate whose first
    /// attempt was rejected by a brake that has since opened again - the flag was off at draft time, or
    /// an admin's autonomy level was briefly lowered. This method does not know whether the linked plan
    /// already ran; the caller must still check the plan's own status before re-executing.
    /// </summary>
    Task<IReadOnlyList<GoalCandidate>> GetApprovedWithPlanAsync(int limit, CancellationToken cancellationToken = default);
}
