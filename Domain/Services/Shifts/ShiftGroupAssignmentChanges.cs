// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Compares the stored group assignments of a shift with the ones a write is about to save and returns every group
/// that is added, removed or re-dated (ValidFrom/ValidUntil changed). Only those groups need to be visible to the
/// caller: an unchanged assignment to a group the caller cannot see (a shift in a visible and a hidden group) must
/// survive a save of the visible shift. Dates are compared by calendar day, and a missing start equals
/// DateTime.MinValue, because the shift resource sends a missing start back as DateTime.MinValue.
/// </summary>
/// <param name="stored">Not deleted group items of the shift as stored; empty for a new shift</param>
/// <param name="requested">Group items the write will save</param>

using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Domain.Services.Shifts;

public static class ShiftGroupAssignmentChanges
{
    public static List<Guid> ChangedGroupIds(IEnumerable<GroupItem> stored, IEnumerable<GroupItem> requested)
    {
        var storedByGroup = stored
            .Where(gi => !gi.IsDeleted)
            .GroupBy(gi => gi.GroupId)
            .ToDictionary(g => g.Key, g => g.First());
        var requestedByGroup = requested
            .Where(gi => !gi.IsDeleted)
            .GroupBy(gi => gi.GroupId)
            .ToDictionary(g => g.Key, g => g.First());

        var added = requestedByGroup.Keys.Where(groupId => !storedByGroup.ContainsKey(groupId));
        var removed = storedByGroup.Keys.Where(groupId => !requestedByGroup.ContainsKey(groupId));
        var redated = requestedByGroup
            .Where(pair => storedByGroup.TryGetValue(pair.Key, out var storedItem) && IsRedated(storedItem, pair.Value))
            .Select(pair => pair.Key);

        return added.Concat(removed).Concat(redated).Distinct().ToList();
    }

    private static bool IsRedated(GroupItem stored, GroupItem requested) =>
        Normalize(stored.ValidFrom) != Normalize(requested.ValidFrom)
        || Normalize(stored.ValidUntil) != Normalize(requested.ValidUntil);

    private static DateTime? Normalize(DateTime? value) =>
        value == null || value.Value == DateTime.MinValue ? null : value.Value.Date;
}
