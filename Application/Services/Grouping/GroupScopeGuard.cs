// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default group scope guard for skill executions: resolves the caller's scope from the
/// GroupVisibility table. Unrestricted when the execution carries no resolvable user
/// (background/system context), when the user is an Admin, or when no visibility rows are
/// configured for the user; otherwise restricted to the user's visible root groups including
/// their whole subtrees.
/// </summary>
/// <param name="userManagementService">Looks up the calling user and checks the Admin role.</param>
/// <param name="groupVisibilityRepository">Reads the per-user group visibility rows.</param>
/// <param name="groupRepository">Provides the root groups to translate visible root ids into names.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Grouping;

public class GroupScopeGuard : IGroupScopeGuard
{
    private readonly IUserManagementService _userManagementService;
    private readonly IGroupVisibilityRepository _groupVisibilityRepository;
    private readonly IGroupRepository _groupRepository;

    public GroupScopeGuard(
        IUserManagementService userManagementService,
        IGroupVisibilityRepository groupVisibilityRepository,
        IGroupRepository groupRepository)
    {
        _userManagementService = userManagementService;
        _groupVisibilityRepository = groupVisibilityRepository;
        _groupRepository = groupRepository;
    }

    public async Task<GroupScopeAccess> GetAccessAsync(
        SkillExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (context.UserId == Guid.Empty)
        {
            return GroupScopeAccess.Unrestricted();
        }

        var user = await _userManagementService.FindUserByIdAsync(context.UserId.ToString());
        if (user == null)
        {
            return GroupScopeAccess.Unrestricted();
        }

        if (await _userManagementService.IsUserInRoleAsync(user, Roles.Admin))
        {
            return GroupScopeAccess.Unrestricted();
        }

        var visibilities = (await _groupVisibilityRepository.GroupVisibilityList(user.Id))
            .Where(gv => string.Equals(gv.AppUserId, user.Id, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (visibilities.Count == 0)
        {
            return GroupScopeAccess.Unrestricted();
        }

        var visibleRootIds = visibilities.Select(gv => gv.GroupId).Distinct().ToList();
        var roots = await _groupRepository.GetRoots();
        var visibleRootNames = roots
            .Where(r => visibleRootIds.Contains(r.Id))
            .Select(r => r.Name)
            .ToList();

        return GroupScopeAccess.Restricted(visibleRootIds, visibleRootNames);
    }
}
