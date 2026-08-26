// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// Deterministic thresholds for learning from helpful proactive messages: each helpful reaction
/// among the user's last RecentReactionsTake reactions on a trigger kind raises that kind's daily
/// dispatch budget for the user by one, capped at MaxDailyBudgetBoost. Dismissals push helpful
/// reactions out of the window, so the boost decays on its own when the user's opinion turns.
/// </summary>
public static class ProactiveHelpfulLearning
{
    public const int RecentReactionsTake = 10;

    public const int MaxDailyBudgetBoost = 3;
}
