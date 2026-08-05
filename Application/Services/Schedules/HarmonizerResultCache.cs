// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Collections.Concurrent;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.ScheduleOptimizer.Harmonizer.Bitmap;

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// Short-lived cache of completed harmonizer runs. The Apply endpoint retrieves the best
/// bitmap and the original input bitmap by JobId. Entries expire after <see cref="TtlMinutes"/>
/// minutes to bound memory.
/// </summary>
public sealed class HarmonizerResultCache
{
    private readonly ConcurrentDictionary<Guid, CacheEntry> _entries = new();

    public int TtlMinutes { get; init; } = 15;

    public void Store(
        Guid jobId,
        HarmonyBitmap originalBitmap,
        HarmonyBitmap bestBitmap,
        Guid? sourceAnalyseToken,
        string subScoreJson = "",
        int stage0Violations = 0,
        ScheduleSnapshotMarker? snapshotMarker = null)
    {
        EvictExpired();
        _entries[jobId] = new CacheEntry(
            originalBitmap,
            bestBitmap,
            sourceAnalyseToken,
            subScoreJson,
            stage0Violations,
            snapshotMarker,
            DateTime.UtcNow.AddMinutes(TtlMinutes));
    }

    public bool TryGet(
        Guid jobId,
        out HarmonyBitmap? originalBitmap,
        out HarmonyBitmap? bestBitmap,
        out Guid? sourceAnalyseToken,
        out string subScoreJson,
        out int stage0Violations,
        out ScheduleSnapshotMarker? snapshotMarker)
    {
        EvictExpired();
        if (_entries.TryGetValue(jobId, out var entry) && entry.ExpiresAt > DateTime.UtcNow)
        {
            originalBitmap = entry.OriginalBitmap;
            bestBitmap = entry.BestBitmap;
            sourceAnalyseToken = entry.SourceAnalyseToken;
            subScoreJson = entry.SubScoreJson;
            stage0Violations = entry.Stage0Violations;
            snapshotMarker = entry.SnapshotMarker;
            return true;
        }

        originalBitmap = null;
        bestBitmap = null;
        sourceAnalyseToken = null;
        subScoreJson = string.Empty;
        stage0Violations = 0;
        snapshotMarker = null;
        return false;
    }

    /// <summary>
    /// Removes and returns the cached result in one atomic step, so two concurrent applies of the same
    /// job cannot both materialise it. Callers that abort must put the entry back via <see cref="Store"/>;
    /// doing so renews the TTL, which is accepted.
    /// </summary>
    /// <param name="jobId">Job whose result is being consumed.</param>
    /// <param name="originalBitmap">Bitmap as loaded before the run.</param>
    /// <param name="bestBitmap">Bitmap the run produced.</param>
    /// <param name="sourceAnalyseToken">Scenario token the run was based on, if any.</param>
    /// <param name="subScoreJson">Serialised score snapshot for the deferred learner.</param>
    /// <param name="stage0Violations">Hard-violation proxy recorded with the run.</param>
    public bool TryTake(
        Guid jobId,
        out HarmonyBitmap? originalBitmap,
        out HarmonyBitmap? bestBitmap,
        out Guid? sourceAnalyseToken,
        out string subScoreJson,
        out int stage0Violations,
        out ScheduleSnapshotMarker? snapshotMarker)
    {
        EvictExpired();
        if (_entries.TryRemove(jobId, out var entry) && entry.ExpiresAt > DateTime.UtcNow)
        {
            originalBitmap = entry.OriginalBitmap;
            bestBitmap = entry.BestBitmap;
            sourceAnalyseToken = entry.SourceAnalyseToken;
            subScoreJson = entry.SubScoreJson;
            stage0Violations = entry.Stage0Violations;
            snapshotMarker = entry.SnapshotMarker;
            return true;
        }

        originalBitmap = null;
        bestBitmap = null;
        sourceAnalyseToken = null;
        subScoreJson = string.Empty;
        stage0Violations = 0;
        snapshotMarker = null;
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
        HarmonyBitmap OriginalBitmap,
        HarmonyBitmap BestBitmap,
        Guid? SourceAnalyseToken,
        string SubScoreJson,
        int Stage0Violations,
        ScheduleSnapshotMarker? SnapshotMarker,
        DateTime ExpiresAt);
}
