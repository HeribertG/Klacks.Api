// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Loads a group by id for a skill and treats a group outside the caller's group scope exactly like a missing one,
/// so a skill answers both with its own not-found text and never reveals that a hidden group exists or what it is called.
/// </summary>
/// <param name="groupRepository">Group store whose Get throws KeyNotFoundException for a missing id</param>
/// <param name="scope">Group scope of the user behind the skill execution</param>
/// <param name="groupId">Id of the group to load</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.Skills;

public static class ScopedGroupLookup
{
    public static async Task<Group?> FindAsync(IGroupRepository groupRepository, GroupScopeAccess scope, Guid groupId)
    {
        Group? group;
        try
        {
            group = await groupRepository.Get(groupId);
        }
        catch (KeyNotFoundException)
        {
            return null;
        }

        return group != null && scope.IsInScope(group) ? group : null;
    }
}
