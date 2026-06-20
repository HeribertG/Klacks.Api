// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// In-memory per-user, per-trigger-kind rate limiter. Counter resets at the UTC midnight
/// boundary by storing the day-key alongside the count. Thread-safe via ConcurrentDictionary.
/// </summary>

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

    private static int BudgetFor(string triggerKind) =>
        PerKindDailyBudget.TryGetValue(triggerKind, out var budget) ? budget : DailyBudgetDefault;

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
        var budget = BudgetFor(triggerKind);
        if (!_state.TryGetValue(key, out var entry) || entry.DayKey != todayKey)
        {
            return budget;
        }
        return Math.Max(0, budget - entry.Count);
    }

    private static string BuildKey(string userId, string triggerKind) => $"{userId}::{triggerKind}";

    private static string TodayKey() => DateTime.UtcNow.ToString("yyyyMMdd");

    private sealed record BudgetEntry(string DayKey, int Count);
}
