// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Clones the shifts of a scenario run: picks the shifts of the group (or the listed ones), copies each under a fresh id
/// with its real root and the child-count snapshot of that root, and resolves the tree links (OriginalId, ParentId,
/// RootId). A link target that is cloned in this run maps to its clone. A target that is not, but whose real shift is
/// represented in this run, maps to that representative's clone: a link to an earlier scenario's clone (a chained run
/// that lists the source scenario's clones while the group brings the real order) resolves through the target's
/// ScenarioSourceShiftId, and a link to a real shift that a listed clone stands in for resolves to that clone's clone.
/// Every resolved id is a clone of THIS run, never a real or foreign-scenario shift, so order families of different worlds
/// never mix (ShiftTreeQuery has no token filter). A target without a representative stays unlinked (null), as before.
/// A listed shift of a variant (key) carries a token but is its own root: its clone points at the key shift, and the key
/// children count in the snapshot like real children. A clone of the source scenario that stands for a shift of a key the
/// new scenario does not carry, and any piece cut from such a clone, is left behind, on the group path and on the listed
/// path; the caller keeps the works on those rows out as well. The cloner reads no scenario row; the tokens come from the
/// caller.
/// </summary>
/// <param name="context">Database context shared with the other scenario collaborators of the scope</param>
/// <param name="groupScopeReader">Resolves the shifts of the group hierarchy in the world the clone copies from</param>

using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

public sealed class ScenarioShiftCloner
{
    private readonly DataBaseContext _context;
    private readonly ScenarioGroupScopeReader _groupScopeReader;

    public ScenarioShiftCloner(DataBaseContext context, ScenarioGroupScopeReader groupScopeReader)
    {
        _context = context;
        _groupScopeReader = groupScopeReader;
    }

    internal async Task<(Dictionary<Guid, Guid> IdMap, List<Shift> Shifts, IReadOnlySet<Guid> LeftBehindShiftIds)> CloneShifts(List<Guid>? groupIds, IReadOnlyCollection<Guid>? additionalShiftIds, Guid token, ScenarioCloneSource source, ScenarioCloneVariants variants, CancellationToken ct)
    {
        var shifts = await LoadShiftsToCloneAsync(groupIds, additionalShiftIds, source, ct);
        var leftBehindShiftIds = await RemoveShiftsOfDroppedVariantsAsync(shifts, variants.DroppedTokens, ct);
        var variantTokens = variants.CarriedTokens;
        var idMap = new Dictionary<Guid, Guid>();

        foreach (var shift in shifts)
        {
            idMap[shift.Id] = Guid.NewGuid();
        }

        var childCountBySource = await LoadChildCountBySourceAsync(shifts, variantTokens, ct);
        var linkMap = await BuildCloneLinkMapAsync(shifts, idMap, ct);

        foreach (var shift in shifts)
        {
            await _context.Shift.AddAsync(BuildClone(shift, idMap[shift.Id], token, variantTokens, childCountBySource, linkMap), ct);
        }

        return (idMap, shifts, leftBehindShiftIds);
    }

    /// <summary>
    /// The root shift a cloned shift stands for: a real shift and a shift of a variant (key) are their own root, a scenario
    /// shift carries its root in ScenarioSourceShiftId (null only for a shift that exists in a scenario alone, which then
    /// has no root to land on).
    /// </summary>
    /// <param name="shift">The shift being cloned</param>
    /// <param name="variantTokens">Tokens of the variants in this run; a shift carrying one of them is a key shift</param>
    internal static Guid? RootShiftIdOf(Shift shift, IReadOnlySet<Guid> variantTokens)
        => shift.AnalyseToken is null || variantTokens.Contains(shift.AnalyseToken.Value)
            ? shift.Id
            : shift.ScenarioSourceShiftId;

    /// <summary>
    /// Adds root aliases to the shift map of a scenario-sourced clone: a real work taken from outside the source range
    /// points at the real root shift, which the clone represents by the copy of the source scenario's clone.
    /// </summary>
    internal static Dictionary<Guid, Guid> WithRootAliases(
        IReadOnlyCollection<Shift> clonedShifts, Dictionary<Guid, Guid> shiftIdMap, ScenarioCloneSource source)
    {
        if (source.Token is null)
        {
            return shiftIdMap;
        }

        var aliased = new Dictionary<Guid, Guid>(shiftIdMap);
        foreach (var shift in clonedShifts)
        {
            if (shift.AnalyseToken is not null && shift.ScenarioSourceShiftId is { } rootId)
            {
                aliased.TryAdd(rootId, shiftIdMap[shift.Id]);
            }
        }

        return aliased;
    }

    /// <summary>
    /// Takes the clones of shifts of the dropped keys out of the list, with the pieces cut from them.
    /// </summary>
    /// <param name="shifts">Shifts of this run; the rows that stay behind are removed from it</param>
    /// <param name="droppedTokens">Tokens of the keys the new scenario does not carry</param>
    /// <returns>Ids of the removed rows</returns>
    private async Task<IReadOnlySet<Guid>> RemoveShiftsOfDroppedVariantsAsync(
        List<Shift> shifts, IReadOnlySet<Guid> droppedTokens, CancellationToken ct)
    {
        if (droppedTokens.Count == 0)
        {
            return new HashSet<Guid>();
        }

        var rootIds = shifts.Select(shift => shift.ScenarioSourceShiftId).OfType<Guid>().Distinct().ToList();
        var droppedRootIds = (await _context.Shift.IgnoreQueryFilters()
                .AsNoTracking()
                .Where(s => rootIds.Contains(s.Id) && s.AnalyseToken != null && droppedTokens.Contains(s.AnalyseToken.Value))
                .Select(s => s.Id)
                .ToListAsync(ct))
            .ToHashSet();

        var leftBehindIds = shifts
            .Where(shift => shift.ScenarioSourceShiftId is { } rootId && droppedRootIds.Contains(rootId))
            .Select(shift => shift.Id)
            .ToHashSet();
        leftBehindIds.UnionWith(shifts
            .Where(shift => shift.OriginalId is { } originalId && leftBehindIds.Contains(originalId))
            .Select(shift => shift.Id)
            .ToList());

        shifts.RemoveAll(shift => leftBehindIds.Contains(shift.Id));
        return leftBehindIds;
    }

    private Task<Dictionary<Guid, int>> LoadChildCountBySourceAsync(
        IReadOnlyCollection<Shift> shifts, IReadOnlySet<Guid> variantTokens, CancellationToken ct)
    {
        var sourceIds = shifts.Select(shift => RootShiftIdOf(shift, variantTokens)).OfType<Guid>().ToList();
        return ScenarioSourceChildCounter.CountAsync(_context, sourceIds, variantTokens, ct);
    }

    private static Shift BuildClone(
        Shift shift,
        Guid cloneId,
        Guid token,
        IReadOnlySet<Guid> variantTokens,
        IReadOnlyDictionary<Guid, int> childCountBySource,
        IReadOnlyDictionary<Guid, Guid> linkMap)
        => new()
        {
            Id = cloneId,
            AnalyseToken = token,
            // Resolve to the real ROOT shift, not the immediate parent, so a clone-of-a-clone
            // (chained AutoWizard W1->W3, or single stages run one after another) still remaps to the real shift on accept. Keep source
            // and snapshot keyed on the same id, or ValidateNoAcceptConflictsAsync throws a false conflict.
            ScenarioSourceShiftId = RootShiftIdOf(shift, variantTokens),
            SourceChildCountSnapshot = RootShiftIdOf(shift, variantTokens) is { } rootId ? childCountBySource.GetValueOrDefault(rootId, 0) : 0,
            CuttingAfterMidnight = shift.CuttingAfterMidnight,
            Abbreviation = shift.Abbreviation,
            Description = shift.Description,
            MacroId = shift.MacroId,
            Name = shift.Name,
            Status = shift.Status,
            AfterShift = shift.AfterShift,
            BeforeShift = shift.BeforeShift,
            EndShift = shift.EndShift,
            FromDate = shift.FromDate,
            StartShift = shift.StartShift,
            UntilDate = shift.UntilDate,
            BriefingTime = shift.BriefingTime,
            DebriefingTime = shift.DebriefingTime,
            TravelTimeAfter = shift.TravelTimeAfter,
            TravelTimeBefore = shift.TravelTimeBefore,
            IsFriday = shift.IsFriday,
            IsHoliday = shift.IsHoliday,
            IsMonday = shift.IsMonday,
            IsSaturday = shift.IsSaturday,
            IsSunday = shift.IsSunday,
            IsThursday = shift.IsThursday,
            IsTuesday = shift.IsTuesday,
            IsWednesday = shift.IsWednesday,
            IsWeekdayAndHoliday = shift.IsWeekdayAndHoliday,
            IsSporadic = shift.IsSporadic,
            SporadicScope = shift.SporadicScope,
            IsTimeRange = shift.IsTimeRange,
            Quantity = shift.Quantity,
            SumEmployees = shift.SumEmployees,
            WorkTime = shift.WorkTime,
            ShiftType = shift.ShiftType,
            OriginalId = ResolveCloneLink(shift.OriginalId, linkMap),
            ParentId = ResolveCloneLink(shift.ParentId, linkMap),
            RootId = ResolveCloneLink(shift.RootId, linkMap),
            Lft = shift.Lft,
            Rgt = shift.Rgt,
            ClientId = shift.ClientId,
            GroupItems = shift.GroupItems.Select(gi => new GroupItem
            {
                Id = Guid.NewGuid(),
                GroupId = gi.GroupId,
                ClientId = gi.ClientId,
                ShiftId = cloneId,
                AnalyseToken = token
            }).ToList()
        };

    private async Task<List<Shift>> LoadShiftsToCloneAsync(List<Guid>? groupIds, IReadOnlyCollection<Guid>? additionalShiftIds, ScenarioCloneSource source, CancellationToken ct)
    {
        var listedIds = additionalShiftIds is { Count: > 0 }
            ? additionalShiftIds.Distinct().ToList()
            : [];

        var shifts = listedIds.Count > 0
            ? await _context.Shift.IgnoreQueryFilters()
                .Where(s => !s.IsDeleted && listedIds.Contains(s.Id))
                .Include(s => s.GroupItems.Where(gi => !gi.IsDeleted))
                .AsNoTracking()
                .ToListAsync(ct)
            : [];

        if (listedIds.Count > 0 && groupIds == null && source.Token is null)
        {
            return shifts;
        }

        IQueryable<Shift> baseShiftQuery = ScenarioSourceFilters.WhereEitherWorld(_context.Shift.Where(s => !s.IsDeleted), source);

        if (groupIds != null)
        {
            var groupShiftIds = await _groupScopeReader.LoadGroupShiftIdsAsync(groupIds, source, ct);

            baseShiftQuery = baseShiftQuery.Where(s => groupShiftIds.Contains(s.Id));
        }

        var baseShifts = await baseShiftQuery
            .Include(s => s.GroupItems.Where(gi => !gi.IsDeleted))
            .AsNoTracking()
            .ToListAsync(ct);

        var knownIds = shifts.Select(s => s.Id).ToHashSet();
        shifts.AddRange(baseShifts.Where(s => !knownIds.Contains(s.Id)));

        // A listed clone of a clone (or a source scenario's clone) stands in for its real root; keeping both would
        // clone one logical shift twice.
        var supersededRootIds = shifts
            .Where(s => s.ScenarioSourceShiftId.HasValue)
            .Select(s => s.ScenarioSourceShiftId!.Value)
            .ToHashSet();
        shifts.RemoveAll(s => supersededRootIds.Contains(s.Id));

        return shifts;
    }

    /// <summary>
    /// Map from every link target the clones need to the clone id of this run that represents it.
    /// </summary>
    /// <param name="shifts">Shifts cloned in this run (real shifts, listed key shifts and listed clones of an earlier scenario)</param>
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
