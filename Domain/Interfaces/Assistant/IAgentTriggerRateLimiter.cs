// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Per-user, per-trigger-kind rate limiter for proactive notifications.
/// Default cap is 5 fires per kind and user per UTC day to avoid notification fatigue.
/// </summary>

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAgentTriggerRateLimiter
{
    bool ShouldFire(string userId, string triggerKind);

    void RecordFire(string userId, string triggerKind);

    int GetRemainingBudget(string userId, string triggerKind);

    /// <summary>
    /// Raises the user's daily budget for the kind by <paramref name="budgetBoost"/> extra fires,
    /// clamped to [0, ProactiveHelpfulLearning.MaxDailyBudgetBoost]. Zero clears the boost.
    /// Recomputed from the persisted reaction history after every stored reaction.
    /// </summary>
    void SetDailyBudgetBoost(string userId, string triggerKind, int budgetBoost);
}
