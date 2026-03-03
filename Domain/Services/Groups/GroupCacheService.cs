// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Klacks.Api.Domain.Services.Groups;

public class GroupCacheService : IGroupCacheService
{
    private readonly IMemoryCache _cache;
    private const string CacheKeyPrefix = "group_hierarchy_";

    public GroupCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public void InvalidateGroupHierarchyCache()
    {
        if (_cache is MemoryCache memoryCache)
        {
            memoryCache.Compact(1.0);
        }
    }

    public void InvalidateGroupCache(Guid groupId)
    {
        var cacheKey = $"{CacheKeyPrefix}{groupId}";
        _cache.Remove(cacheKey);
    }
}
