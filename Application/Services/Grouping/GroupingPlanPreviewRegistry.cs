// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Remembers which user has seen the preview (apply=false) of which grouping plan and in which chat turn,
/// so that apply_grouping_plan accepts apply=true only after the same user previewed the same full plan
/// fingerprint within GroupingFeasibilityDefaults.PreviewValidityMinutes and in an earlier turn: an apply
/// in the turn of the preview is SameTurn, because the user has not answered the preview yet. A repeated
/// preview keeps the turn of the first one and restarts the validity window, so a model that previews
/// again in the confirming turn does not block that turn. Paths without a chat turn (null) never count as
/// the same turn. Kept in the host memory cache: lost on restart (the user previews again), and every
/// entry declares size 1 because the cache has a size limit. Recording reads the earlier entry and writes
/// the new one under one lock, so two concurrent previews of the same plan cannot both miss the earlier
/// entry and let the later turn overwrite the first one; reads and Forget take the same lock.
/// </summary>
/// <param name="cache">Host-wide memory cache.</param>

using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Application.Interfaces.Grouping;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Microsoft.Extensions.Caching.Memory;

namespace Klacks.Api.Application.Services.Grouping;

public sealed class GroupingPlanPreviewRegistry : IGroupingPlanPreviewRegistry
{
    private const int EntrySize = 1;
    private const char KeySeparator = ':';
    private const string GuidFormat = "N";

    private readonly IMemoryCache _cache;
    private readonly object _gate = new();

    public GroupingPlanPreviewRegistry(IMemoryCache cache)
    {
        _cache = cache;
    }

    public void RecordPreview(Guid userId, string fingerprint, Guid? turnId)
    {
        var key = CacheKey(userId, fingerprint);
        lock (_gate)
        {
            var entry = _cache.TryGetValue(key, out GroupingPlanPreviewEntry? earlier) && earlier is not null
                ? earlier
                : new GroupingPlanPreviewEntry(turnId);
            _cache.Set(
                key,
                entry,
                new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(GroupingFeasibilityDefaults.PreviewValidityMinutes))
                    .SetSize(EntrySize));
        }
    }

    public GroupingPreviewStatus GetPreviewStatus(Guid userId, string fingerprint, Guid? currentTurnId)
    {
        GroupingPlanPreviewEntry? entry;
        lock (_gate)
        {
            _cache.TryGetValue(CacheKey(userId, fingerprint), out entry);
        }

        if (entry is null)
        {
            return GroupingPreviewStatus.Missing;
        }

        return currentTurnId is Guid turn && entry.TurnId == turn
            ? GroupingPreviewStatus.SameTurn
            : GroupingPreviewStatus.Confirmable;
    }

    public void Forget(Guid userId, string fingerprint)
    {
        lock (_gate)
        {
            _cache.Remove(CacheKey(userId, fingerprint));
        }
    }

    private static string CacheKey(Guid userId, string fingerprint) =>
        GroupingFeasibilityDefaults.PreviewCacheKeyPrefix + userId.ToString(GuidFormat) + KeySeparator + fingerprint;
}
