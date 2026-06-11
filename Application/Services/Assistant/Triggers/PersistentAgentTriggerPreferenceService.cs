// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF-backed implementation of IAgentTriggerPreferenceService. Replaces the in-memory
/// store from S8 so settings survive restarts. Synchronous methods (Mute/Snooze/...)
/// block on the async repository to keep the public interface unchanged — repository
/// calls are very short, so the trade-off is acceptable for now; a fully async
/// preference API is a follow-up refactor.
/// </summary>
/// <param name="repository">Persistent EF-backed store.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class PersistentAgentTriggerPreferenceService : IAgentTriggerPreferenceService
{
    private static readonly Dictionary<string, int> SeverityRank = new(StringComparer.OrdinalIgnoreCase)
    {
        [AgentTriggerSeverity.Low] = 0,
        [AgentTriggerSeverity.Medium] = 1,
        [AgentTriggerSeverity.High] = 2
    };

    private readonly IAgentTriggerPreferenceRepository _repository;

    public PersistentAgentTriggerPreferenceService(IAgentTriggerPreferenceRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> IsAllowedAsync(string userId, string triggerKind, string severity)
    {
        var pref = await GetPreferenceAsync(userId, triggerKind);
        if (pref.Muted) return false;
        if (pref.SnoozedUntilUtc.HasValue && pref.SnoozedUntilUtc.Value > DateTime.UtcNow) return false;
        return RankOf(severity) >= RankOf(pref.MinimumSeverity);
    }

    public async Task<AgentTriggerPreference> GetPreferenceAsync(string userId, string triggerKind)
    {
        var row = await _repository.GetAsync(userId, triggerKind);
        if (row == null)
        {
            return new AgentTriggerPreference(userId, triggerKind, false, null, AgentTriggerSeverity.Low);
        }
        return new AgentTriggerPreference(row.UserId, row.TriggerKind, row.Muted, row.SnoozedUntilUtc, row.MinimumSeverity);
    }

    public async Task MuteAsync(string userId, string triggerKind, bool muted)
    {
        await MutateAsync(userId, triggerKind, row => row.Muted = muted);
    }

    public async Task SnoozeAsync(string userId, string triggerKind, DateTime? snoozedUntilUtc)
    {
        await MutateAsync(userId, triggerKind, row => row.SnoozedUntilUtc = snoozedUntilUtc);
    }

    public async Task SetMinimumSeverityAsync(string userId, string triggerKind, string severity)
    {
        if (!SeverityRank.ContainsKey(severity))
        {
            throw new ArgumentException($"Unknown severity '{severity}'", nameof(severity));
        }
        await MutateAsync(userId, triggerKind, row => row.MinimumSeverity = severity);
    }

    private async Task MutateAsync(string userId, string triggerKind, Action<AgentTriggerPreferenceRow> change)
    {
        var existing = await _repository.GetAsync(userId, triggerKind);
        var row = existing ?? new AgentTriggerPreferenceRow
        {
            UserId = userId,
            TriggerKind = triggerKind,
            Muted = false,
            SnoozedUntilUtc = null,
            MinimumSeverity = AgentTriggerSeverity.Low,
            CreateTime = DateTime.UtcNow
        };
        change(row);
        await _repository.UpsertAsync(row);
    }

    private static int RankOf(string severity) => SeverityRank.GetValueOrDefault(severity, 0);
}
