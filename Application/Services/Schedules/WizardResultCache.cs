// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Collections.Concurrent;
using Klacks.Api.Infrastructure.Hubs;
using Klacks.ScheduleOptimizer.Models;

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// Short-lived cache of completed wizard scenarios. The Apply endpoint retrieves results by JobId.
/// Entries expire after <see cref="TtlMinutes"/> minutes to bound memory.
/// Also stores the AnalyseToken so Apply can propagate it through Work creation.
/// </summary>
public sealed class WizardResultCache
{
    private readonly ConcurrentDictionary<Guid, CacheEntry> _entries = new();

    public int TtlMinutes { get; init; } = 15;

    public void Store(
        Guid jobId,
        CoreScenario scenario,
        Guid? analyseToken,
        IReadOnlyList<WizardEscalationDto>? escalations = null)
    {
        EvictExpired();
        _entries[jobId] = new CacheEntry(
            scenario,
            analyseToken,
            escalations ?? [],
            DateTime.UtcNow.AddMinutes(TtlMinutes));
    }

    public bool TryGet(
        Guid jobId,
        out CoreScenario? scenario,
        out Guid? analyseToken,
        out IReadOnlyList<WizardEscalationDto> escalations)
    {
        EvictExpired();
        if (_entries.TryGetValue(jobId, out var entry) && entry.ExpiresAt > DateTime.UtcNow)
        {
            scenario = entry.Scenario;
            analyseToken = entry.AnalyseToken;
            escalations = entry.Escalations;
            return true;
        }

        scenario = null;
        analyseToken = null;
        escalations = [];
        return false;
    }

    public void Invalidate(Guid jobId) => _entries.TryRemove(jobId, out _);

    private void EvictExpired()
    {
        var now = DateTime.UtcNow;
        foreach (var kv in _entries)
        {
            if (kv.Value.ExpiresAt <= now)
            {
                _entries.TryRemove(kv.Key, out _);
            }
        }
    }

    private sealed record CacheEntry(
        CoreScenario Scenario,
        Guid? AnalyseToken,
        IReadOnlyList<WizardEscalationDto> Escalations,
        DateTime ExpiresAt);
}
