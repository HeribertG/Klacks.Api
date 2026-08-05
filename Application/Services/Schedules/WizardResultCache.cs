// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Collections.Concurrent;
using Klacks.Api.Application.DTOs.Schedules;
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
        IReadOnlyList<WizardEscalationDto>? escalations = null,
        string subScoreJson = "",
        int stage0Violations = 0)
    {
        EvictExpired();
        _entries[jobId] = new CacheEntry(
            scenario,
            analyseToken,
            escalations ?? [],
            subScoreJson,
            stage0Violations,
            DateTime.UtcNow.AddMinutes(TtlMinutes));
    }

    public bool TryGet(
        Guid jobId,
        out CoreScenario? scenario,
        out Guid? analyseToken,
        out IReadOnlyList<WizardEscalationDto> escalations,
        out string subScoreJson,
        out int stage0Violations)
    {
        EvictExpired();
        if (_entries.TryGetValue(jobId, out var entry) && entry.ExpiresAt > DateTime.UtcNow)
        {
            scenario = entry.Scenario;
            analyseToken = entry.AnalyseToken;
            escalations = entry.Escalations;
            subScoreJson = entry.SubScoreJson;
            stage0Violations = entry.Stage0Violations;
            return true;
        }

        scenario = null;
        analyseToken = null;
        escalations = [];
        subScoreJson = string.Empty;
        stage0Violations = 0;
        return false;
    }

    /// <summary>
    /// Removes and returns the cached scenario in one atomic step, so two concurrent applies of the same
    /// job cannot both materialise it. Callers that abort must put the entry back via <see cref="Store"/>;
    /// doing so renews the TTL, which is accepted.
    /// </summary>
    /// <param name="jobId">Job whose result is being consumed.</param>
    /// <param name="scenario">The scenario the run produced.</param>
    /// <param name="analyseToken">Scenario token the run was based on, if any.</param>
    /// <param name="escalations">Stage-1 relaxations logged during the run.</param>
    /// <param name="subScoreJson">Serialised score snapshot for the deferred learner.</param>
    /// <param name="stage0Violations">Hard-violation count recorded with the run.</param>
    public bool TryTake(
        Guid jobId,
        out CoreScenario? scenario,
        out Guid? analyseToken,
        out IReadOnlyList<WizardEscalationDto> escalations,
        out string subScoreJson,
        out int stage0Violations)
    {
        EvictExpired();
        if (_entries.TryRemove(jobId, out var entry) && entry.ExpiresAt > DateTime.UtcNow)
        {
            scenario = entry.Scenario;
            analyseToken = entry.AnalyseToken;
            escalations = entry.Escalations;
            subScoreJson = entry.SubScoreJson;
            stage0Violations = entry.Stage0Violations;
            return true;
        }

        scenario = null;
        analyseToken = null;
        escalations = [];
        subScoreJson = string.Empty;
        stage0Violations = 0;
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
        string SubScoreJson,
        int Stage0Violations,
        DateTime ExpiresAt);
}
