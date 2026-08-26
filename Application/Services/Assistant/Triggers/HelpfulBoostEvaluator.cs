// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Deterministic helpful learning: counts the helpful reactions among the user's last
/// ProactiveHelpfulLearning.RecentReactionsTake reactions on a trigger kind and hands that count
/// to the rate limiter as the kind's daily budget boost for this user (the limiter caps it at
/// MaxDailyBudgetBoost). Running after every reaction keeps the boost symmetric: dismissals push
/// helpful reactions out of the window and the boost sinks back on the next recomputation. The
/// mute_suggestion kind is excluded because the meta question about muting must never earn
/// extra dispatch slots.
/// </summary>
/// <param name="dispatchRepository">Loads the user's most recent reactions for the kind.</param>
/// <param name="rateLimiter">Receives the recomputed per-user, per-kind budget boost.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class HelpfulBoostEvaluator : IHelpfulBoostEvaluator
{
    private readonly IProactiveTriggerDispatchRepository _dispatchRepository;
    private readonly IAgentTriggerRateLimiter _rateLimiter;

    public HelpfulBoostEvaluator(
        IProactiveTriggerDispatchRepository dispatchRepository,
        IAgentTriggerRateLimiter rateLimiter)
    {
        _dispatchRepository = dispatchRepository;
        _rateLimiter = rateLimiter;
    }

    public async Task EvaluateAsync(string userId, string triggerKind, CancellationToken cancellationToken = default)
    {
        if (string.Equals(triggerKind, AgentTriggerKinds.MuteSuggestion, StringComparison.Ordinal))
        {
            return;
        }

        var recentReactions = await _dispatchRepository.GetRecentReactionsAsync(
            userId, triggerKind, ProactiveHelpfulLearning.RecentReactionsTake, cancellationToken);
        var helpfulCount = recentReactions.Count(row => row.Reaction == ProactiveReaction.Helpful);
        _rateLimiter.SetDailyBudgetBoost(userId, triggerKind, helpfulCount);
    }
}
