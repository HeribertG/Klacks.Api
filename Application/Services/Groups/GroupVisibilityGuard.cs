// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Group-visibility check for group writes. Resolves the caller's scope from IGroupVisibilityService, but
/// treats a request without a calling user as unrestricted first: the service reports Restricted([], []) in
/// that case, which would lock out background jobs (same rule as ClientGroupFilterService).
/// </summary>
/// <param name="groupVisibilityService">Determines admin status and the visible groups including subgroups</param>
/// <param name="userService">Identifies the caller; no user means a background job, not a restricted user</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.Services.Groups;

public class GroupVisibilityGuard : IGroupVisibilityGuard
{
    private readonly IGroupVisibilityService _groupVisibilityService;
    private readonly IUserService _userService;

    public GroupVisibilityGuard(IGroupVisibilityService groupVisibilityService, IUserService userService)
    {
        _groupVisibilityService = groupVisibilityService;
        _userService = userService;
    }

    public async Task<bool> IsUnrestrictedAsync(CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync();
        return scope.IsUnrestricted;
    }

    public Task<bool> IsGroupVisibleAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        return AreAllGroupsVisibleAsync([groupId], cancellationToken);
    }

    public async Task<bool> AreAllGroupsVisibleAsync(
        IReadOnlyCollection<Guid> groupIds, CancellationToken cancellationToken = default)
    {
        if (groupIds.Count == 0)
        {
            return true;
        }

        var scope = await ResolveScopeAsync();
        if (scope.IsUnrestricted)
        {
            return true;
        }

        var visibleIds = scope.VisibleGroupIds.ToHashSet();
        return groupIds.All(visibleIds.Contains);
    }

    private async Task<GroupVisibilityScope> ResolveScopeAsync()
    {
        if (string.IsNullOrEmpty(_userService.GetIdString()))
        {
            return GroupVisibilityScope.Unrestricted();
        }

        return await _groupVisibilityService.GetVisibilityScopeAsync();
    }
}
