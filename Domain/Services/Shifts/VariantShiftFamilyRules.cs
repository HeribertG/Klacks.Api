// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Rules a real order family must meet before it is copied into a variant (key). The groups of all its rows must lie in
/// the variant's group hierarchy and there must be at least one, because a shift in a variant needs a group; every row
/// needs a customer, because the shift form refuses a shift in a variant without one. A row is valid in the variant's
/// period when its validity intersects the period; an open end intersects every period that does not end before the row
/// starts. A shift in a variant lies with start and end inside the period, so a copied row is clipped to it and an open end
/// becomes the end of the period. RowsInPeriod names the rows that are copied: a row outside the period stays behind, which
/// is safe as long as no row inside points at it. A date cut gives a child dates disjoint from its parent, so a child is
/// no subset of its parent; a row inside that hangs on a row outside has no place in the copy, and nulling the link would
/// leave it without a root, so the family is refused. A family without a plannable row inside the period is refused too.
/// A cut piece in a variant starts with fresh copies of the live group links of its parent. The root of a family is its
/// sealed order: a sealed order is its own root, every other row names it in OriginalId, and a row without OriginalId belongs
/// to no family.
/// </summary>
/// <param name="family">Live rows of one order family, loaded with their live group items</param>
/// <param name="allowedGroupIds">Group ids of the variant's group and its subgroups</param>
/// <param name="row">Row of a family whose validity is compared with the period</param>
/// <param name="from">First day of a shift</param>
/// <param name="until">Last day of a shift; null means open-ended</param>
/// <param name="periodFrom">First day of the variant's period</param>
/// <param name="periodUntil">Last day of the variant's period</param>
/// <param name="source">Shift whose live group items are copied, loaded with its group items</param>
/// <param name="shiftId">Shift the copies belong to</param>
/// <param name="variantToken">Token the copies carry</param>
/// <param name="id">Id of the row whose root is asked for</param>
/// <param name="status">Status of that row</param>
/// <param name="originalId">OriginalId of that row</param>
/// <param name="shift">Row whose root is asked for</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Shifts;

public static class VariantShiftFamilyRules
{
    public static Shift? FindSealedOrder(IEnumerable<Shift> family)
        => family.FirstOrDefault(shift => shift.Status == ShiftStatus.SealedOrder);

    public static Guid? FamilyRootOf(Guid id, ShiftStatus status, Guid? originalId)
        => status == ShiftStatus.SealedOrder ? id : originalId;

    public static Guid? FamilyRootOf(Shift shift)
        => FamilyRootOf(shift.Id, shift.Status, shift.OriginalId);

    public static Guid FamilyRootOrSelf(Shift shift)
        => FamilyRootOf(shift) ?? shift.Id;

    public static bool HasOnlyAllowedGroups(IEnumerable<Shift> family, IReadOnlySet<Guid> allowedGroupIds)
    {
        var groupIds = family
            .SelectMany(shift => shift.GroupItems)
            .Where(item => !item.IsDeleted)
            .Select(item => item.GroupId)
            .Distinct()
            .ToList();

        return groupIds.Count > 0 && groupIds.All(allowedGroupIds.Contains);
    }

    public static List<GroupItem> CopyLiveGroupItems(Shift source, Guid shiftId, Guid variantToken)
        => source.GroupItems
            .Where(item => !item.IsDeleted)
            .Select(item => new GroupItem
            {
                Id = Guid.NewGuid(),
                ShiftId = shiftId,
                GroupId = item.GroupId,
                ValidFrom = item.ValidFrom,
                ValidUntil = item.ValidUntil,
                AnalyseToken = variantToken
            })
            .ToList();

    public static bool IntersectsPeriod(Shift row, DateOnly periodFrom, DateOnly periodUntil)
        => ClippedFrom(row, periodFrom) <= ClippedUntil(row, periodUntil);

    public static DateOnly ClippedFrom(Shift row, DateOnly periodFrom)
        => row.FromDate > periodFrom ? row.FromDate : periodFrom;

    public static DateOnly ClippedUntil(Shift row, DateOnly periodUntil)
        => row.UntilDate is { } until && until < periodUntil ? until : periodUntil;

    public static bool HasCustomer(IEnumerable<Shift> family)
        => family.All(row => row.ClientId.HasValue);

    public static bool LiesInsidePeriod(DateOnly from, DateOnly? until, DateOnly periodFrom, DateOnly periodUntil)
        => from >= periodFrom && until is { } end && from <= end && end <= periodUntil;

    public static List<Shift>? RowsInPeriod(IReadOnlyCollection<Shift> family, DateOnly periodFrom, DateOnly periodUntil)
    {
        var rows = family.Where(row => IntersectsPeriod(row, periodFrom, periodUntil)).ToList();
        var leftBehindIds = family.Except(rows).Select(row => row.Id).ToHashSet();

        var hangsOnLeftBehindRow = rows.Any(row => new[] { row.OriginalId, row.ParentId, row.RootId }
            .Any(link => link is { } target && leftBehindIds.Contains(target)));
        var hasPlannableRow = rows.Any(row => row.Status != ShiftStatus.SealedOrder);

        return !hangsOnLeftBehindRow && hasPlannableRow ? rows : null;
    }
}
