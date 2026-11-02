// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Copies real order families (sealed order, plannable copy, cut pieces) into a variant (key). Every row gets a new id from
/// one id map, the variant token on the shift and its group items, no scenario source, an empty child-count snapshot and no
/// nested-set values; OriginalId, ParentId and RootId follow the id map and become null when their target is not part of
/// the copied rows, so the cut hierarchy of a copy never points back into the real plan. The one link back is
/// CopiedFromShiftId on the copied sealed order: it names the real order the copy was made from, the only row of a family
/// that keeps its id through cuts and resets. Every row is clipped to the variant's period, an open end becomes its last
/// day. Expenses and required qualifications are copied by the new shift id and carry no token. The source rows are never
/// changed.
/// </summary>
/// <param name="shifts">Live rows of the families to copy, loaded with their live group items, expenses and required qualifications</param>
/// <param name="variantToken">Token of the variant that receives the copies</param>
/// <param name="periodFrom">First day of the variant's period</param>
/// <param name="periodUntil">Last day of the variant's period</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Schedules;

namespace Klacks.Api.Domain.Services.Shifts;

public static class VariantShiftFamilyCloner
{
    public static List<Shift> Clone(IReadOnlyCollection<Shift> shifts, Guid variantToken, DateOnly periodFrom, DateOnly periodUntil)
    {
        var idMap = shifts.ToDictionary(shift => shift.Id, _ => Guid.NewGuid());

        return shifts.Select(shift => CloneOne(shift, variantToken, periodFrom, periodUntil, idMap)).ToList();
    }

    private static Shift CloneOne(
        Shift source, Guid variantToken, DateOnly periodFrom, DateOnly periodUntil, IReadOnlyDictionary<Guid, Guid> idMap)
    {
        var clone = ShiftCloner.Clone(source);
        clone.Id = idMap[source.Id];
        clone.AnalyseToken = variantToken;
        clone.ScenarioSourceShiftId = null;
        clone.CopiedFromShiftId = source.Status == ShiftStatus.SealedOrder ? source.Id : null;
        clone.FromDate = VariantShiftFamilyRules.ClippedFrom(source, periodFrom);
        clone.UntilDate = VariantShiftFamilyRules.ClippedUntil(source, periodUntil);
        clone.SourceChildCountSnapshot = 0;
        clone.Lft = null;
        clone.Rgt = null;
        clone.OriginalId = Resolve(source.OriginalId, idMap);
        clone.ParentId = Resolve(source.ParentId, idMap);
        clone.RootId = Resolve(source.RootId, idMap);

        foreach (var groupItem in clone.GroupItems)
        {
            groupItem.Id = Guid.NewGuid();
            groupItem.ShiftId = clone.Id;
            groupItem.AnalyseToken = variantToken;
        }

        foreach (var expense in clone.ShiftExpenses)
        {
            expense.Id = Guid.NewGuid();
            expense.ShiftId = clone.Id;
        }

        clone.RequiredQualifications = ShiftRequirementMaterializer.CopyAll(
            source.RequiredQualifications.Where(requirement => !requirement.IsDeleted), clone.Id);

        return clone;
    }

    private static Guid? Resolve(Guid? target, IReadOnlyDictionary<Guid, Guid> idMap)
        => target.HasValue && idMap.TryGetValue(target.Value, out var copyId) ? copyId : null;
}
