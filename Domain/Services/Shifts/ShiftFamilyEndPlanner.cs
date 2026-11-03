// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Pure rule that sorts the rows of a shift family for an end date. A derived row (plannable copy or cut piece) starting
/// after the day goes with its whole subtree, walked through ParentId because Lft and Rgt can be empty. The head row is
/// never removed. Every remaining row without an end or with a later end gets the day; a row ending earlier stays as it is.
/// </summary>
/// <param name="family">The live rows of the family, head row included</param>
/// <param name="headId">Id of the head row: the sealed order, or the container (a family without an order)</param>
/// <param name="untilDate">The inclusive last day of the family</param>

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Shifts;

public static class ShiftFamilyEndPlanner
{
    public static ShiftFamilyEndPlan Build(IReadOnlyList<Shift> family, Guid headId, DateOnly untilDate)
    {
        var childrenByParent = family
            .Where(row => row.ParentId.HasValue)
            .ToLookup(row => row.ParentId!.Value);

        var removed = new Dictionary<Guid, Shift>();
        foreach (var row in family.Where(row => row.Id != headId && row.FromDate > untilDate))
        {
            CollectSubtree(row, childrenByParent, removed);
        }

        var rowsToTrim = family
            .Where(row => !removed.ContainsKey(row.Id) && (row.UntilDate == null || row.UntilDate > untilDate))
            .ToList();

        return new ShiftFamilyEndPlan(rowsToTrim, removed.Values.ToList());
    }

    private static void CollectSubtree(Shift row, ILookup<Guid, Shift> childrenByParent, Dictionary<Guid, Shift> removed)
    {
        if (!removed.TryAdd(row.Id, row))
        {
            return;
        }

        foreach (var child in childrenByParent[row.Id])
        {
            CollectSubtree(child, childrenByParent, removed);
        }
    }
}
