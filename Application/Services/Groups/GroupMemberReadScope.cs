// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Clears the member items of groups the caller cannot see from group read results (owner decision 2026-11-07:
/// member lists are person data). The group rows themselves stay readable, so their ClientsCount drops to 0;
/// unrestricted callers (admin, no calling user, installation without groups) keep every member list.
/// </summary>

using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Application.Interfaces;

namespace Klacks.Api.Application.Services.Groups;

public static class GroupMemberReadScope
{
    /// <param name="groupVisibilityGuard">Supplies the caller's visibility scope, resolved once per call</param>
    /// <param name="groups">Mapped groups whose member items are cleared when the group is hidden</param>
    /// <param name="cancellationToken">Cancels the scope resolution</param>
    public static async Task HideMembersOfHiddenGroupsAsync(
        IGroupVisibilityGuard groupVisibilityGuard,
        ICollection<GroupResource> groups,
        CancellationToken cancellationToken)
    {
        if (groups.Count == 0)
        {
            return;
        }

        var scope = await groupVisibilityGuard.GetScopeAsync(cancellationToken);
        if (scope.IsUnrestricted)
        {
            return;
        }

        var visibleGroupIds = scope.VisibleGroupIds.ToHashSet();
        foreach (var group in groups.Where(group => !visibleGroupIds.Contains(group.Id)))
        {
            group.GroupItems = [];
        }
    }
}
