// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// In-memory implementation of IAgentTriggerPreferenceService. Trades persistence for
/// simplicity in S8; the REST endpoint can already use it and the persistent EF-backed
/// store is a follow-up swap behind the same interface.
/// </summary>

using System.Collections.Concurrent;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class InMemoryAgentTriggerPreferenceService : IAgentTriggerPreferenceService
{
    private static readonly Dictionary<string, int> SeverityRank = new(StringComparer.OrdinalIgnoreCase)
    {
        [AgentTriggerSeverity.Low] = 0,
        [AgentTriggerSeverity.Medium] = 1,
        [AgentTriggerSeverity.High] = 2
    };

    private readonly ConcurrentDictionary<string, AgentTriggerPreference> _state = new();

    public Task<bool> IsAllowedAsync(string userId, string triggerKind, string severity)
    {
        var pref = GetPreferenceCore(userId, triggerKind);
        if (pref.Muted) return Task.FromResult(false);
        if (pref.SnoozedUntilUtc.HasValue && pref.SnoozedUntilUtc.Value > DateTime.UtcNow) return Task.FromResult(false);
        return Task.FromResult(RankOf(severity) >= RankOf(pref.MinimumSeverity));
    }

    public Task<AgentTriggerPreference> GetPreferenceAsync(string userId, string triggerKind)
    {
        return Task.FromResult(GetPreferenceCore(userId, triggerKind));
    }

    public Task MuteAsync(string userId, string triggerKind, bool muted)
    {
        Mutate(userId, triggerKind, p => p with { Muted = muted });
        return Task.CompletedTask;
    }

    public Task SnoozeAsync(string userId, string triggerKind, DateTime? snoozedUntilUtc)
    {
        Mutate(userId, triggerKind, p => p with { SnoozedUntilUtc = snoozedUntilUtc });
        return Task.CompletedTask;
    }

    public Task SetMinimumSeverityAsync(string userId, string triggerKind, string severity)
    {
        if (!SeverityRank.ContainsKey(severity))
        {
            throw new ArgumentException($"Unknown severity '{severity}'", nameof(severity));
        }
        Mutate(userId, triggerKind, p => p with { MinimumSeverity = severity });
        return Task.CompletedTask;
    }

    private AgentTriggerPreference GetPreferenceCore(string userId, string triggerKind)
    {
        return _state.TryGetValue(Key(userId, triggerKind), out var pref)
            ? pref
            : new AgentTriggerPreference(userId, triggerKind, false, null, AgentTriggerSeverity.Low);
    }

    private void Mutate(string userId, string triggerKind, Func<AgentTriggerPreference, AgentTriggerPreference> change)
    {
        _state.AddOrUpdate(
            Key(userId, triggerKind),
            _ => change(new AgentTriggerPreference(userId, triggerKind, false, null, AgentTriggerSeverity.Low)),
            (_, prev) => change(prev));
    }

    private static string Key(string userId, string triggerKind) => $"{userId}::{triggerKind}";

    private static int RankOf(string severity) => SeverityRank.GetValueOrDefault(severity, 0);
}
