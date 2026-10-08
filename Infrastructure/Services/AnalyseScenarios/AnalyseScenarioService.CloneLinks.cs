// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves the tree links (OriginalId, ParentId, RootId) of the shifts a scenario run clones. A link target that is
/// cloned in this run maps to its clone. A target that is not, but whose real shift is represented in this run, maps to
/// that representative's clone: a link to an earlier scenario's clone (a chained run that lists the source scenario's
/// clones while the group brings the real order) resolves through the target's ScenarioSourceShiftId, and a link to a
/// real shift that a listed clone stands in for resolves to that clone's clone. Every resolved id is a clone of THIS
/// run, never a real or foreign-scenario shift, so order families of different worlds never mix (ShiftTreeQuery has no
/// token filter). A target without a representative stays unlinked (null), as before.
/// </summary>

using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

public partial class AnalyseScenarioService
{
    /// <summary>
    /// Map from every link target the clones need to the clone id of this run that represents it.
    /// </summary>
    /// <param name="shifts">Shifts cloned in this run (real shifts and listed clones of an earlier scenario)</param>
    /// <param name="idMap">Shift id to its clone id in this run</param>
    private async Task<Dictionary<Guid, Guid>> BuildCloneLinkMapAsync(
        IReadOnlyCollection<Shift> shifts, IReadOnlyDictionary<Guid, Guid> idMap, CancellationToken ct)
    {
        var linkMap = new Dictionary<Guid, Guid>(idMap);
        var cloneByRealShift = new Dictionary<Guid, Guid>();
        foreach (var shift in shifts)
        {
            if (shift.ScenarioSourceShiftId is null)
            {
                cloneByRealShift[shift.Id] = idMap[shift.Id];
            }
            else
            {
                cloneByRealShift.TryAdd(shift.ScenarioSourceShiftId.Value, idMap[shift.Id]);
            }
        }

        var unresolvedTargets = shifts
            .SelectMany(shift => new[] { shift.OriginalId, shift.ParentId, shift.RootId })
            .Where(target => target.HasValue && !idMap.ContainsKey(target.Value))
            .Select(target => target!.Value)
            .Distinct()
            .ToList();
        if (unresolvedTargets.Count == 0)
        {
            return linkMap;
        }

        var realShiftOfTarget = await _context.Shift.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(s => unresolvedTargets.Contains(s.Id))
            .Select(s => new { s.Id, RealShiftId = s.ScenarioSourceShiftId ?? s.Id })
            .ToDictionaryAsync(s => s.Id, s => s.RealShiftId, ct);

        foreach (var target in unresolvedTargets)
        {
            var realShiftId = realShiftOfTarget.GetValueOrDefault(target, target);
            if (cloneByRealShift.TryGetValue(realShiftId, out var representative))
            {
                linkMap[target] = representative;
            }
        }

        return linkMap;
    }

    private static Guid? ResolveCloneLink(Guid? target, IReadOnlyDictionary<Guid, Guid> linkMap)
        => target.HasValue && linkMap.TryGetValue(target.Value, out var clone) ? clone : null;
}
