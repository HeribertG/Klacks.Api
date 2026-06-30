// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves the planning audience (users in the Admin or Authorised role) via the ASP.NET Identity
/// UserManager. The result is cached briefly because role membership changes rarely and a recurring
/// trigger scan can ask for the audience many times per tick.
/// </summary>
/// <param name="userManager">Identity user manager used to enumerate role members.</param>
/// <param name="cache">Short-lived cache for the resolved user-id set.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Authentification;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class PlanningAudienceResolver : IPlanningAudienceResolver
{
    private const string CacheKey = "assistant:planning-audience-user-ids";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly UserManager<AppUser> _userManager;
    private readonly IMemoryCache _cache;

    public PlanningAudienceResolver(UserManager<AppUser> userManager, IMemoryCache cache)
    {
        _userManager = userManager;
        _cache = cache;
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
}
