// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves the planning audience (users in the Admin or Authorised role) via the ASP.NET Identity
/// UserManager. The result is cached briefly because role membership changes rarely and a recurring
/// trigger scan can ask for the audience many times per tick.
/// </summary>
/// <param name="userManager">Identity user manager used to enumerate role members.</param>
/// <param name="cache">Short-lived cache for the resolved user-id set.</param>
/// <param name="groupVisibilityRepository">Per-user GroupVisibility rows for group-scoped audience filtering.</param>
/// <param name="groupRepository">Resolves an event's group to its Nested Set root.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Authentification;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class PlanningAudienceResolver : IPlanningAudienceResolver
{
    private const string CacheKey = "assistant:planning-audience-user-ids";
    private const string AdminCacheKey = "assistant:admin-audience-user-ids";
    private const string GroupAudienceCacheKeyPrefix = "assistant:planning-audience-user-ids:group:";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly UserManager<AppUser> _userManager;
    private readonly IMemoryCache _cache;
    private readonly IGroupVisibilityRepository _groupVisibilityRepository;
    private readonly IGroupRepository _groupRepository;

    public PlanningAudienceResolver(
        UserManager<AppUser> userManager,
        IMemoryCache cache,
        IGroupVisibilityRepository groupVisibilityRepository,
        IGroupRepository groupRepository)
    {
        _userManager = userManager;
        _cache = cache;
        _groupVisibilityRepository = groupVisibilityRepository;
        _groupRepository = groupRepository;
    }

    public async Task<IReadOnlySet<string>> GetPlanningUserIdsAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey, out IReadOnlySet<string>? cached) && cached is not null)
        {
            return cached;
        }

        var admins = await _userManager.GetUsersInRoleAsync(Roles.Admin);
        var authorised = await _userManager.GetUsersInRoleAsync(Roles.Authorised);

        var ids = admins
            .Concat(authorised)
            .Select(u => u.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        _cache.Set(CacheKey, (IReadOnlySet<string>)ids, new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(CacheDuration)
            .SetSize(1));
        return ids;
    }

    public async Task<IReadOnlySet<string>> GetAdminUserIdsAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(AdminCacheKey, out IReadOnlySet<string>? cached) && cached is not null)
        {
            return cached;
        }

        var admins = await _userManager.GetUsersInRoleAsync(Roles.Admin);

        var ids = admins
            .Select(u => u.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        _cache.Set(AdminCacheKey, (IReadOnlySet<string>)ids, new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(CacheDuration)
            .SetSize(1));
        return ids;
    }

    public async Task<IReadOnlySet<string>> GetPlanningUserIdsForGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var adminIds = await GetAdminUserIdsAsync(cancellationToken);

        // A deleted/unknown group cannot be verified against anyone's GroupVisibility, so only the
        // always-unrestricted admins are returned instead of guessing either way.
        var rootId = await ResolveRootIdAsync(groupId);
        if (rootId is null)
        {
            return adminIds;
        }

        var cacheKey = GroupAudienceCacheKeyPrefix + rootId.Value;
        if (_cache.TryGetValue(cacheKey, out IReadOnlySet<string>? cached) && cached is not null)
        {
            return cached;
        }

        var plannerIds = await GetPlanningUserIdsAsync(cancellationToken);
        var scopedIds = new HashSet<string>(adminIds, StringComparer.OrdinalIgnoreCase);

        foreach (var userId in plannerIds)
        {
            if (adminIds.Contains(userId))
            {
                continue;
            }

            var visibleRootIds = await ReadVisibleRootIdsAsync(userId);
            if (visibleRootIds.Contains(rootId.Value))
            {
                scopedIds.Add(userId);
            }
        }

        _cache.Set(cacheKey, (IReadOnlySet<string>)scopedIds, new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(CacheDuration)
            .SetSize(1));
        return scopedIds;
    }

    private async Task<Guid?> ResolveRootIdAsync(Guid groupId)
    {
        var group = await _groupRepository.GetNoTracking(groupId);
        return group is null ? null : group.Root ?? group.Id;
    }

    private async Task<IReadOnlySet<Guid>> ReadVisibleRootIdsAsync(string userId)
    {
        var rows = await _groupVisibilityRepository.GroupVisibilityList(userId);

        // GroupVisibilityList also injects synthetic all-roots rows for every Admin (via
        // ReviseAdminVisibility), even when the queried user has zero rows of their own. Filtering
        // on AppUserId is what keeps a no-row Authorised planner fail-closed instead of silently
        // inheriting admin visibility.
        return rows
            .Where(gv => string.Equals(gv.AppUserId, userId, StringComparison.OrdinalIgnoreCase))
            .Select(gv => gv.GroupId)
            .ToHashSet();
    }
}
