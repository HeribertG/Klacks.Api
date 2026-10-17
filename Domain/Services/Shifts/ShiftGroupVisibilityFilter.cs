// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Applies the shift group-visibility rule to queries and loaded shifts: a shift is visible when the restriction is
/// null (unrestricted caller), when it belongs to no group at all, or when at least one of its groups is in the
/// restriction. An empty restriction therefore keeps only group-less shifts (fail-closed).
/// </summary>
/// <param name="visibleGroupIds">Flat set of allowed group ids from IShiftGroupFilterService.ResolveShiftListGroupIdsAsync; null = unrestricted</param>

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Shifts;

public static class ShiftGroupVisibilityFilter
{
    public static IQueryable<Shift> Apply(IQueryable<Shift> query, IReadOnlyCollection<Guid>? visibleGroupIds)
    {
        if (visibleGroupIds == null)
        {
            return query;
        }

        var allowedGroupIds = visibleGroupIds.ToList();
        return query.Where(s =>
            !s.GroupItems.Any(gi => !gi.IsDeleted)
            || s.GroupItems.Any(gi => !gi.IsDeleted && allowedGroupIds.Contains(gi.GroupId)));
    }

    public static bool IsVisible(Shift shift, IReadOnlyCollection<Guid>? visibleGroupIds)
    {
        if (visibleGroupIds == null)
        {
            return true;
        }

        var groupIds = shift.GroupItems
            .Where(gi => !gi.IsDeleted)
            .Select(gi => gi.GroupId)
            .ToList();

        return groupIds.Count == 0 || groupIds.Any(visibleGroupIds.Contains);
    }
}
