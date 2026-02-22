using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Klacks.Api.Infrastructure.Services.Groups;

public class GroupClientService : IGetAllClientIdsFromGroupAndSubgroups
{
    private readonly DataBaseContext _context;
    private readonly IMemoryCache _cache;
    private const string CacheKeyPrefix = "group_hierarchy_";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public GroupClientService(DataBaseContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<List<Guid>> GetAllClientIdsFromGroupAndSubgroups(Guid groupId)
    {
        var groupExists = await _context.Group.AnyAsync(g => g.Id == groupId);
        if (!groupExists)
        {
            throw new KeyNotFoundException($"Group with ID {groupId} was not found");
        }

        var allGroupIds = await GetAllSubGroupIds(new List<Guid> { groupId });
        return await GetClientIdsForGroups(allGroupIds);
    }

    public async Task<List<Guid>> GetAllClientIdsFromGroupsAndSubgroupsFromList(List<Guid> groupIds)
    {
        if (groupIds == null || !groupIds.Any())
        {
            return new List<Guid>();
        }

        var existingGroups = await _context.Group
            .Where(g => groupIds.Contains(g.Id))
            .Select(g => g.Id)
            .ToListAsync();

        var missingGroups = groupIds.Except(existingGroups).ToList();
        if (missingGroups.Any())
        {
            throw new KeyNotFoundException($"Groups with IDs {string.Join(", ", missingGroups)} were not found");
        }

        var allGroupIds = await GetAllSubGroupIds(existingGroups);
        return await GetClientIdsForGroups(allGroupIds);
    }

    private async Task<HashSet<Guid>> GetAllSubGroupIds(List<Guid> initialGroupIds)
    {
        if (!initialGroupIds.Any())
        {
            return new HashSet<Guid>();
        }

        var groupIdsString = string.Join("','", initialGroupIds);

        var sql = $@"
            WITH RECURSIVE group_hierarchy AS (
                SELECT id, parent
                FROM ""group""
                WHERE id::text IN ('{groupIdsString}')

                UNION ALL

                SELECT g.id, g.parent
                FROM ""group"" g
                INNER JOIN group_hierarchy gh ON g.parent = gh.id
            )
            SELECT DISTINCT id::text AS value FROM group_hierarchy";

        var allGroupIdsString = await _context.Database
            .SqlQueryRaw<string>(sql)
            .ToListAsync();

        var allGroupIds = allGroupIdsString.Select(id => Guid.Parse(id)).ToList();

        return new HashSet<Guid>(allGroupIds);
    }

    public async Task<HashSet<Guid>> GetAllGroupIdsIncludingSubgroups(Guid groupId)
    {
        var cacheKey = $"{CacheKeyPrefix}{groupId}";

        if (_cache.TryGetValue(cacheKey, out HashSet<Guid>? cachedResult) && cachedResult != null)
        {
            return cachedResult;
        }

        var groupExists = await _context.Group.AnyAsync(g => g.Id == groupId);
        if (!groupExists)
        {
            throw new KeyNotFoundException($"Group with ID {groupId} was not found");
        }

        var result = await GetAllSubGroupIds(new List<Guid> { groupId });

        _cache.Set(cacheKey, result, CacheDuration);

        return result;
    }

    public async Task<HashSet<Guid>> GetAllGroupIdsIncludingSubgroupsFromList(List<Guid> groupIds)
    {
        if (groupIds == null || !groupIds.Any())
        {
            return new HashSet<Guid>();
        }

        var sortedIds = groupIds.OrderBy(x => x).ToList();
        var cacheKey = $"{CacheKeyPrefix}{string.Join("_", sortedIds)}";

        if (_cache.TryGetValue(cacheKey, out HashSet<Guid>? cachedResult) && cachedResult != null)
        {
            return cachedResult;
        }

        var existingGroups = await _context.Group
            .Where(g => groupIds.Contains(g.Id))
            .Select(g => g.Id)
            .ToListAsync();

        var missingGroups = groupIds.Except(existingGroups).ToList();
        if (missingGroups.Any())
        {
            throw new KeyNotFoundException($"Groups with IDs {string.Join(", ", missingGroups)} were not found");
        }

        var result = await GetAllSubGroupIds(existingGroups);

        _cache.Set(cacheKey, result, CacheDuration);

        return result;
    }

    private async Task<List<Guid>> GetClientIdsForGroups(HashSet<Guid> groupIds)
    {
        return await _context.GroupItem
            .AsNoTracking()
            .Where(gi => gi.ClientId.HasValue && groupIds.Contains(gi.GroupId))
            .Select(gi => gi.ClientId!.Value)
            .Distinct()
            .ToListAsync();
    }
}