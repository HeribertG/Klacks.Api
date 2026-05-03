// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Collections.Concurrent;
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

    public void Store(Guid jobId, HarmonyBitmap originalBitmap, HarmonyBitmap bestBitmap, Guid? sourceAnalyseToken)
    {
        EvictExpired();
        _entries[jobId] = new CacheEntry(originalBitmap, bestBitmap, sourceAnalyseToken, DateTime.UtcNow.AddMinutes(TtlMinutes));
    }

    public bool TryGet(Guid jobId, out HarmonyBitmap? originalBitmap, out HarmonyBitmap? bestBitmap, out Guid? sourceAnalyseToken)
    {
        EvictExpired();
        if (_entries.TryGetValue(jobId, out var entry) && entry.ExpiresAt > DateTime.UtcNow)
        {
            originalBitmap = entry.OriginalBitmap;
            bestBitmap = entry.BestBitmap;
            sourceAnalyseToken = entry.SourceAnalyseToken;
            return true;
        }

        originalBitmap = null;
        bestBitmap = null;
        sourceAnalyseToken = null;
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
        DateTime ExpiresAt);
}
