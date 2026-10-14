// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The plan-view rule for a shift: it is visible when it belongs to no group at all, when the caller is unrestricted, or
/// when it belongs to at least one group the caller may see. A shift outside that rule is answered exactly like a
/// missing one, so its name never leaks.
/// </summary>
/// <param name="shift">Shift loaded with its group items</param>
/// <param name="groupVisibilityGuard">Resolves the caller's group visibility</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Skills;

internal static class ShiftGroupVisibility
{
    public static async Task<bool> IsVisibleAsync(
        Shift shift, IGroupVisibilityGuard groupVisibilityGuard, CancellationToken cancellationToken)
    {
        var groupIds = shift.GroupItems
            .Where(item => !item.IsDeleted)
            .Select(item => item.GroupId)
            .Distinct()
            .ToList();
        if (groupIds.Count == 0 || await groupVisibilityGuard.IsUnrestrictedAsync(cancellationToken))
        {
            return true;
        }

        foreach (var groupId in groupIds)
        {
            if (await groupVisibilityGuard.IsGroupVisibleAsync(groupId, cancellationToken))
            {
                return true;
            }
        }

        return false;
    }
}
