// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Holds the result summary of the company day's installation-wide grouping analysis in the shared
/// memory cache. The detector computes it at most once per company day, so a report the user fixed
/// elsewhere (group UI, single skills) stops being reminded of by the next company day; a chat analysis
/// with the default scope overwrites it, so the next hourly tick announces the fresh state to the admins
/// without a second analysis. A successful apply of a grouping plan removes it, so the next tick
/// recomputes and the ledger row of the old report resolves. Lost on restart by design (one
/// recomputation). The host cache has a size limit, so every entry declares size 1; high priority keeps
/// compaction from evicting the day's snapshot before cheaper entries. Every Remove bumps one store-wide
/// generation; a snapshot computed from a read at generation g is stored only while the generation is
/// still g, so an analysis that overlapped a successful apply cannot put its pre-apply result back after
/// the apply removed the snapshot. Generation check and write happen under one lock.
/// </summary>
/// <param name="cache">Host-wide memory cache.</param>

using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Application.Interfaces.Grouping;
using Klacks.Api.Domain.Constants;
using Microsoft.Extensions.Caching.Memory;

namespace Klacks.Api.Application.Services.Grouping;

public sealed class GroupingFeasibilityDailySnapshotStore : IGroupingFeasibilityDailySnapshotStore
{
    private const int EntrySize = 1;

    private readonly IMemoryCache _cache;
    private readonly object _gate = new();
    private long _generation;

    public GroupingFeasibilityDailySnapshotStore(IMemoryCache cache)
    {
        _cache = cache;
    }

    public long CurrentGeneration
    {
        get
        {
            lock (_gate)
            {
                return _generation;
            }
        }
    }

    public GroupingFeasibilityDailySnapshot? TryGet(string dayKey, out long generation)
    {
        lock (_gate)
        {
            generation = _generation;
            return _cache.TryGetValue(CacheKey(dayKey), out GroupingFeasibilityDailySnapshot? snapshot) ? snapshot : null;
        }
    }

    public bool Set(string dayKey, GroupingFeasibilityDailySnapshot snapshot, long expectedGeneration)
    {
        lock (_gate)
        {
            if (_generation != expectedGeneration)
            {
                return false;
            }

            _cache.Set(
                CacheKey(dayKey),
                snapshot,
                new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromDays(GroupingFeasibilityDefaults.DailySnapshotRetentionDays))
                    .SetPriority(CacheItemPriority.High)
                    .SetSize(EntrySize));
            return true;
        }
    }

    public void Remove(string dayKey)
    {
        lock (_gate)
        {
            _generation++;
            _cache.Remove(CacheKey(dayKey));
        }
    }

    private static string CacheKey(string dayKey) => GroupingFeasibilityDefaults.DailySnapshotCacheKeyPrefix + dayKey;
}
