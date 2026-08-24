// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF-backed implementation of IAgentConditionScopeResolver. Queries GroupVisibility directly rather
/// than going through IGroupVisibilityRepository.GroupVisibilityList: that repository method also runs
/// ReviseAdminVisibility, which loads every AppUser plus every Group to inject synthetic rows for every
/// Admin - correct for the visibility-editing UI it was built for, but far more expensive than this
/// per-chat-turn read needs, since the admin case is already handled by the role check below. Cached for
/// 5 minutes per user, same duration as the sibling PlanningAudienceResolver caches: this resolver is
/// consulted on every chat turn that carries a user id, so an uncached role lookup plus a GroupVisibility
/// query per turn would be wasted work between role/visibility changes, which are rare.
/// </summary>
/// <param name="context">Database context, queried directly for GroupVisibility rows.</param>
/// <param name="userManager">Identity user manager used to resolve the given user's roles.</param>
/// <param name="cache">Short-lived per-user cache for the resolved scope.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class AgentConditionScopeResolver : IAgentConditionScopeResolver
{
    private const string CacheKeyPrefix = "assistant:agent-condition-scope:";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly DataBaseContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly IMemoryCache _cache;

    public AgentConditionScopeResolver(DataBaseContext context, UserManager<AppUser> userManager, IMemoryCache cache)
    {
        _context = context;
        _userManager = userManager;
        _cache = cache;
    }

    public async Task<AgentConditionVisibilityScope> ResolveAsync(string userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeyPrefix + userId;
        if (_cache.TryGetValue(cacheKey, out AgentConditionVisibilityScope? cached) && cached is not null)
        {
            return cached;
        }

        var scope = await ResolveUncachedAsync(userId, cancellationToken);
        _cache.Set(cacheKey, scope, new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(CacheDuration)
            .SetSize(1));
        return scope;
    }

    private async Task<AgentConditionVisibilityScope> ResolveUncachedAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return AgentConditionVisibilityScope.NotAPlanner();
        }

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains(Roles.Admin))
        {
            return AgentConditionVisibilityScope.Unrestricted();
        }

        if (!roles.Contains(Roles.Authorised))
        {
            return AgentConditionVisibilityScope.NotAPlanner();
        }

        var visibleRootIds = await _context.GroupVisibility
            .AsNoTracking()
            .Where(gv => gv.AppUserId == userId)
            .Select(gv => gv.GroupId)
            .ToListAsync(cancellationToken);

        return AgentConditionVisibilityScope.Restricted(visibleRootIds.ToHashSet());
    }
}
