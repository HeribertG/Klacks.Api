// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Deterministic dismiss-streak learning: after a user dismissed the same trigger kind
/// DismissStreakThreshold times in a row (no other reaction in between), fires a targeted
/// MuteSuggestionTriggerEvent through the normal trigger pipeline, whose per-kind dedup key
/// guarantees the mute question is asked at most once per user and kind. The mute_suggestion
/// kind itself is excluded so dismissing the question never triggers another question.
/// </summary>
/// <param name="dispatchRepository">Loads the user's most recent reactions for the kind.</param>
/// <param name="triggerService">Dispatches the mute suggestion through preferences, dedup and budget.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class DismissStreakEvaluator : IDismissStreakEvaluator
{
    private readonly IProactiveTriggerDispatchRepository _dispatchRepository;
    private readonly IAgentTriggerService _triggerService;

    public DismissStreakEvaluator(
        IProactiveTriggerDispatchRepository dispatchRepository,
        IAgentTriggerService triggerService)
    {
        _dispatchRepository = dispatchRepository;
        _triggerService = triggerService;
    }

    public async Task EvaluateAsync(string userId, string triggerKind, CancellationToken cancellationToken = default)
    {
        if (string.Equals(triggerKind, AgentTriggerKinds.MuteSuggestion, StringComparison.Ordinal))
        {
            return;
        }

        if (!Guid.TryParse(userId, out var targetUserId))
        {
            return;
        }

        var recentReactions = await _dispatchRepository.GetRecentReactionsAsync(
            userId, triggerKind, ProactiveDismissLearning.RecentReactionsTake, cancellationToken);
        var streak = CountLeadingDismissals(recentReactions);
        if (streak == ProactiveDismissLearning.DismissStreakThreshold)
        {
            await _triggerService.OnEventAsync(new MuteSuggestionTriggerEvent(triggerKind, targetUserId), cancellationToken);
        }
    }

    private static int CountLeadingDismissals(IReadOnlyList<ProactiveTriggerDispatchRow> recentReactions)
    {
        var streak = 0;
        foreach (var row in recentReactions)
        {
            if (row.Reaction != ProactiveReaction.Dismissed)
            {
                break;
            }

            streak++;
        }

        return streak;
    }
}
