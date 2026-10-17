// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves which groups restrict the shift views for the current caller. Group visibility is a security
/// boundary: a restricted caller without any visible group gets an empty (fail-closed) list, not an unrestricted
/// one, and a selected group the caller may not see is never honoured.
/// </summary>
/// <param name="groupVisibility">Determines admin status and the caller's visibility scope</param>
/// <param name="user">Identifies the caller; no user means a background job, which stays unrestricted</param>

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Domain.Services.ShiftSchedule;

public class ShiftGroupFilterService : IShiftGroupFilterService
{
    private readonly IGroupVisibilityService _groupVisibility;
    private readonly IUserService _user;

    public ShiftGroupFilterService(IGroupVisibilityService groupVisibility, IUserService user)
    {
        _groupVisibility = groupVisibility;
        _user = user;
    }

    public async Task<List<Guid>> GetVisibleGroupIdsAsync(Guid? selectedGroupId)
    {
        if (selectedGroupId.HasValue && await IsGroupSelectableAsync(selectedGroupId.Value))
        {
            return [selectedGroupId.Value];
        }

        if (await _groupVisibility.IsAdmin())
        {
            return [];
        }

        return await _groupVisibility.ReadVisibleRootIdList();
    }

    public Task<List<Guid>?> ResolveShiftScheduleGroupIdsAsync(Guid? selectedGroupId)
        => ResolveRestrictionAsync(selectedGroupId, scope => scope.VisibleRootIds);

    public Task<List<Guid>?> ResolveShiftListGroupIdsAsync(Guid? selectedGroupId)
        => ResolveRestrictionAsync(selectedGroupId, scope => scope.VisibleGroupIds);

    private async Task<List<Guid>?> ResolveRestrictionAsync(
        Guid? selectedGroupId, Func<GroupVisibilityScope, IEnumerable<Guid>> unselectedGroupIds)
    {
        var scope = await ResolveScopeAsync();
        if (scope.IsUnrestricted)
        {
            return selectedGroupId.HasValue ? [selectedGroupId.Value] : null;
        }

        if (selectedGroupId.HasValue)
        {
            return scope.VisibleGroupIds.Contains(selectedGroupId.Value) ? [selectedGroupId.Value] : [];
        }

        return unselectedGroupIds(scope).ToList();
    }

    private async Task<bool> IsGroupSelectableAsync(Guid groupId)
    {
        var scope = await ResolveScopeAsync();
        return scope.IsUnrestricted || scope.VisibleGroupIds.Contains(groupId);
    }

    private async Task<GroupVisibilityScope> ResolveScopeAsync()
    {
        if (string.IsNullOrEmpty(_user.GetIdString()))
        {
            return GroupVisibilityScope.Unrestricted();
        }

        return await _groupVisibility.GetVisibilityScopeAsync();
    }
}
