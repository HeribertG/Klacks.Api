// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/**
 * Shared rule of the group delete validators: a group and its descendants may only be deleted while no planned
 * work exists for any shift of the subtree. An unknown group passes, the handler answers it as not found.
 */

using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Schedules;

namespace Klacks.Api.Application.Validation.Groups;

public static class GroupSubtreePlannedWorksRule
{
    public const string ErrorMessageKey = "group.validation.has-planned-works";

    public static async Task<bool> HasNoPlannedWorksAsync(
        Guid groupId,
        IShiftRepository shiftRepository,
        IGroupHierarchyService groupHierarchyService,
        CancellationToken cancellationToken)
    {
        IEnumerable<Guid> groupIds;
        try
        {
            var subtree = await groupHierarchyService.GetDescendantsAsync(groupId, includeParent: true);
            groupIds = subtree.Select(g => g.Id);
        }
        catch (KeyNotFoundException)
        {
            return true;
        }

        return !await shiftRepository.HasWorksForAnyGroupAsync(groupIds, null, cancellationToken);
    }
}
