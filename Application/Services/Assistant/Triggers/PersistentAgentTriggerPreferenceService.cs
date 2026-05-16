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

    public bool IsAllowed(string userId, string triggerKind, string severity)
    {
        var pref = GetPreference(userId, triggerKind);
        if (pref.Muted) return false;
        if (pref.SnoozedUntilUtc.HasValue && pref.SnoozedUntilUtc.Value > DateTime.UtcNow) return false;
        return RankOf(severity) >= RankOf(pref.MinimumSeverity);
    }

    public AgentTriggerPreference GetPreference(string userId, string triggerKind)
    {
        var row = _repository.GetAsync(userId, triggerKind).GetAwaiter().GetResult();
        if (row == null)
        {
            return new AgentTriggerPreference(userId, triggerKind, false, null, AgentTriggerSeverity.Low);
        }
        return new AgentTriggerPreference(row.UserId, row.TriggerKind, row.Muted, row.SnoozedUntilUtc, row.MinimumSeverity);
    }

    public void Mute(string userId, string triggerKind, bool muted)
    {
        Mutate(userId, triggerKind, row => row.Muted = muted);
    }

    public void Snooze(string userId, string triggerKind, DateTime? snoozedUntilUtc)
    {
        Mutate(userId, triggerKind, row => row.SnoozedUntilUtc = snoozedUntilUtc);
    }

    public void SetMinimumSeverity(string userId, string triggerKind, string severity)
    {
        if (!SeverityRank.ContainsKey(severity))
        {
            throw new ArgumentException($"Unknown severity '{severity}'", nameof(severity));
        }
        Mutate(userId, triggerKind, row => row.MinimumSeverity = severity);
    }

    private void Mutate(string userId, string triggerKind, Action<AgentTriggerPreferenceRow> change)
    {
        var existing = _repository.GetAsync(userId, triggerKind).GetAwaiter().GetResult();
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
        _repository.UpsertAsync(row).GetAwaiter().GetResult();
    }

    private static int RankOf(string severity) => SeverityRank.GetValueOrDefault(severity, 0);
}
