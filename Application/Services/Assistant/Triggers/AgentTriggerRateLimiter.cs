// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// In-memory per-user, per-trigger-kind rate limiter. Counter resets at the UTC midnight
/// boundary by storing the day-key alongside the count. Thread-safe via ConcurrentDictionary.
/// A per-user, per-kind budget boost learned from helpful reactions (see HelpfulBoostEvaluator)
/// is added on top of the base budget; like the daily counters it lives in memory only and is
/// re-established from the persisted reaction history on the user's next reaction.
/// </summary>
/// <param name="timeProvider">Clock the day-key is derived from, injected so a test can drive it.</param>

using System.Collections.Concurrent;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class AgentTriggerRateLimiter : IAgentTriggerRateLimiter
{
    private const int DailyBudgetDefault = 5;

    // Per-kind overrides of the daily budget. Curiosity questions are kept deliberately rare
    // (at most one per user per day) to stay helpful rather than nagging.
    private static readonly IReadOnlyDictionary<string, int> PerKindDailyBudget =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            [AgentTriggerKinds.CuriosityQuestion] = 1,
        };

    private readonly ConcurrentDictionary<string, BudgetEntry> _state = new();
    private readonly ConcurrentDictionary<string, int> _budgetBoosts = new();
    private readonly TimeProvider _timeProvider;

    public AgentTriggerRateLimiter(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    private static int BaseBudgetFor(string triggerKind) =>
        PerKindDailyBudget.TryGetValue(triggerKind, out var budget) ? budget : DailyBudgetDefault;

    private int BudgetFor(string userId, string triggerKind)
    {
        var boost = _budgetBoosts.TryGetValue(BuildKey(userId, triggerKind), out var value) ? value : 0;
        return BaseBudgetFor(triggerKind) + boost;
    }

    public bool ShouldFire(string userId, string triggerKind)
    {
        return GetRemainingBudget(userId, triggerKind) > 0;
    }

    public void RecordFire(string userId, string triggerKind)
    {
        var key = BuildKey(userId, triggerKind);
        var todayKey = TodayKey();
        _state.AddOrUpdate(
            key,
            _ => new BudgetEntry(todayKey, 1),
            (_, prev) => prev.DayKey == todayKey
                ? prev with { Count = prev.Count + 1 }
                : new BudgetEntry(todayKey, 1));
    }

    public int GetRemainingBudget(string userId, string triggerKind)
    {
        var key = BuildKey(userId, triggerKind);
        var todayKey = TodayKey();
        var budget = BudgetFor(userId, triggerKind);
        if (!_state.TryGetValue(key, out var entry) || entry.DayKey != todayKey)
        {
            return budget;
        }
        return Math.Max(0, budget - entry.Count);
    }

    public void SetDailyBudgetBoost(string userId, string triggerKind, int budgetBoost)
    {
        var key = BuildKey(userId, triggerKind);
        var clampedBoost = Math.Clamp(budgetBoost, 0, ProactiveHelpfulLearning.MaxDailyBudgetBoost);
        if (clampedBoost == 0)
        {
            _budgetBoosts.TryRemove(key, out _);
            return;
        }

        _budgetBoosts[key] = clampedBoost;
    }

    private static string BuildKey(string userId, string triggerKind) => $"{userId}::{triggerKind}";

    private string TodayKey() => _timeProvider.GetUtcNow().UtcDateTime.ToString("yyyyMMdd");

    private sealed record BudgetEntry(string DayKey, int Count);
}
