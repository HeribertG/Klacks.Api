// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Schedules;

/// <summary>
/// The one rule for whose required-qualification rows apply to a shift: walk the chain shift -> cut ancestors
/// (ParentId) -> plannable copy (the shift with Status OriginalShift of the same order) -> sealed order (OriginalId);
/// the nearest link that has rows of its own wins, and rows are never merged across links. A planner change on the
/// plannable copy or on a cut piece therefore replaces what the order asked for, while a piece without rows (the cut
/// dialog creates pieces without rows) stays protected by the nearest link above it. "Has rows" means any active row,
/// optional ones included; callers filter IsMandatory only after resolving. Deliberately different from shift
/// preferences (<see cref="ShiftScopeExpander"/>), where Blacklist wins across all levels.
/// Since 2026-10-08 the write paths keep this from surprising the planner (<see cref="ShiftRequirementMaterializer"/>):
/// the first own row written on a shift that inherits turns the inherited rows into own rows first, and the cut dialog
/// gives every new piece a copy of what applied to its parent. The copy is taken at cut time, so a later change on the
/// parent no longer reaches pieces cut before it. Remaining limit: when a planner removes ALL rows of the plannable copy
/// (or of a piece), the next link applies again, in the end the sealed order, which the UI does not show. A scenario
/// clone whose order was not cloned has OriginalId = null and sees only its own and its cloned ancestors' rows.
/// </summary>
public static class ShiftRequirementSourceResolver
{
    /// <summary>
    /// The shift whose rows apply, per requested shift; a shift without any link carrying rows is left out.
    /// </summary>
    /// <param name="shiftIds">Shifts that are staffed</param>
    /// <param name="rows">Tree rows of their order families</param>
    /// <param name="shiftsWithOwnRows">Shifts that carry at least one active requirement row of their own</param>
    public static IReadOnlyDictionary<Guid, Guid> ResolveSources(
        IReadOnlyCollection<Guid> shiftIds,
        IReadOnlyCollection<ShiftTreeRow> rows,
        IReadOnlySet<Guid> shiftsWithOwnRows)
    {
        var index = ShiftTreeIndex.Build(rows);
        var sources = new Dictionary<Guid, Guid>();
        foreach (var shiftId in shiftIds.Distinct())
        {
            foreach (var link in ChainOf(shiftId, index))
            {
                if (shiftsWithOwnRows.Contains(link))
                {
                    sources[shiftId] = link;
                    break;
                }
            }
        }

        return sources;
    }

    private static IEnumerable<Guid> ChainOf(Guid shiftId, ShiftTreeIndex index)
    {
        var visited = new HashSet<Guid> { shiftId };
        yield return shiftId;

        if (!index.ById.TryGetValue(shiftId, out var shift))
        {
            yield break;
        }

        var current = shift;
        while (current.ParentId is { } parentId && visited.Add(parentId))
        {
            yield return parentId;
            if (!index.ById.TryGetValue(parentId, out var parent))
            {
                break;
            }

            current = parent;
        }

        if (shift.OriginalId is not { } orderId)
        {
            yield break;
        }

        var plannableCopy = index.ByOrder.GetValueOrDefault(orderId, [])
            .Where(id => index.ById[id].Status == ShiftStatus.OriginalShift)
            .OrderBy(id => id)
            .FirstOrDefault();
        if (plannableCopy != Guid.Empty && visited.Add(plannableCopy))
        {
            yield return plannableCopy;
        }

        if (visited.Add(orderId))
        {
            yield return orderId;
        }
    }
}
