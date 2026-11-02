// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Copies existing real order families into a variant (key) and only stages the copies; the handler commits. A family is
/// the sealed order plus every live row whose OriginalId points at it (plannable copy, cut pieces), found with one
/// projection query and one family query, and is copied once whichever member is named: every row that overlaps the variant
/// period, clipped to it (VariantShiftFamilyRules.RowsInPeriod). A selection is refused before anything is staged when a
/// shift is missing, belongs to a variant or scenario, is an open order, has no group or a group outside the variant
/// hierarchy, has no customer, lies outside the variant period, or cannot be cut down to the period (no plannable row
/// inside it, or a row inside it hanging on a row outside it). A whole group copies the visible sealed families of its
/// hierarchy that fit and counts every family one of these rules leaves out, and open orders, as skipped; soft-deleted rows
/// are ignored. Client shift preferences are not copied, and a repeated copy is not detected.
/// </summary>
/// <param name="context">Database context shared with the other scenario collaborators of the scope</param>
/// <param name="groupScopeReader">Resolves a group hierarchy and the shifts it holds in the real plan</param>
/// <param name="visibilityGuard">Leaves out the shifts the caller may not see when a whole group is copied</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.ScenarioVariants;
using Klacks.Api.Application.Services.Shifts;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Shifts;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

public sealed class ScenarioVariantShiftCopier : IScenarioVariantShiftCopier
{
    public const string NotRealMessage = "Only shifts of the real plan can be copied into a variant";

    public const string OpenOrderMessage = "An open order must be sealed before it can be copied into a variant";

    public const string OrphanMessage = "The shift does not belong to a sealed order and cannot be copied into a variant";

    public const string GroupsOutsideMessage = "A copied shift needs at least one group, and every group must lie in the variant's group or its subgroups";

    public const string OutsidePeriodMessage = "A copied shift must be valid inside the variant's period";

    public const string PartsOutsidePeriodMessage = "The shift has no plannable part inside the variant's period, or a part inside it hangs on a part outside it";

    private readonly DataBaseContext _context;
    private readonly ScenarioGroupScopeReader _groupScopeReader;
    private readonly IShiftVisibilityGuard _visibilityGuard;

    public ScenarioVariantShiftCopier(
        DataBaseContext context, ScenarioGroupScopeReader groupScopeReader, IShiftVisibilityGuard visibilityGuard)
    {
        _context = context;
        _groupScopeReader = groupScopeReader;
        _visibilityGuard = visibilityGuard;
    }

    public async Task<CopyShiftsToVariantResult> StageCopyOfShiftsAsync(
        AnalyseScenario variant, IReadOnlyCollection<Guid> variantGroupIds, IReadOnlyCollection<Guid> shiftIds, CancellationToken cancellationToken = default)
    {
        var requested = shiftIds.Distinct().ToList();
        var stored = await LoadRefsAsync(requested, cancellationToken);
        var roots = ResolveRoots(requested, stored);
        var families = await LoadFamiliesAsync(roots, cancellationToken);
        var allowedGroupIds = variantGroupIds.ToHashSet();

        foreach (var root in roots)
        {
            EnsureCopyable(root, families, variant, allowedGroupIds);
        }

        await StageAsync(families, roots, variant, cancellationToken);

        return new CopyShiftsToVariantResult(roots.Count, 0);
    }

    public async Task<CopyShiftsToVariantResult> StageCopyOfGroupAsync(
        AnalyseScenario variant, IReadOnlyCollection<Guid> variantGroupIds, Guid sourceGroupId, CancellationToken cancellationToken = default)
    {
        var sourceGroupIds = await _groupScopeReader.GetGroupHierarchyIdsAsync(sourceGroupId, cancellationToken);
        var candidateIds = await _groupScopeReader.LoadGroupShiftIdsAsync(sourceGroupIds, ScenarioCloneSource.RealPlan, cancellationToken);
        var visibleIds = await _visibilityGuard.FilterVisibleAsync(candidateIds, cancellationToken);
        var stored = (await LoadRefsAsync(visibleIds.ToList(), cancellationToken)).Values
            .Where(shift => shift.IsReal)
            .ToList();

        var members = stored.Where(shift => shift.Status != ShiftStatus.OriginalOrder).ToList();
        var skipped = stored.Count - members.Count + members.Count(shift => RootOf(shift) is null);
        var roots = members.Select(member => RootOf(member)).OfType<Guid>().Distinct().ToList();

        var families = await LoadFamiliesAsync(roots, cancellationToken);
        var allowedGroupIds = variantGroupIds.ToHashSet();
        var copyable = roots.Where(root => Fits(root, families, variant, allowedGroupIds)).ToList();

        await StageAsync(families, copyable, variant, cancellationToken);

        return new CopyShiftsToVariantResult(copyable.Count, skipped + roots.Count - copyable.Count);
    }

    private static List<Guid> ResolveRoots(IReadOnlyCollection<Guid> requested, IReadOnlyDictionary<Guid, ShiftRef> stored)
    {
        var roots = new List<Guid>();

        foreach (var id in requested)
        {
            if (!stored.TryGetValue(id, out var shift))
            {
                throw new KeyNotFoundException(string.Format(ShiftVisibilityMessages.ShiftNotFound, id));
            }

            if (!shift.IsReal)
            {
                throw new InvalidRequestException(NotRealMessage);
            }

            if (shift.Status == ShiftStatus.OriginalOrder)
            {
                throw new InvalidRequestException(OpenOrderMessage);
            }

            roots.Add(RootOf(shift) ?? throw new InvalidRequestException(OrphanMessage));
        }

        return roots.Distinct().ToList();
    }

    private static Guid? RootOf(ShiftRef shift)
        => VariantShiftFamilyRules.FamilyRootOf(shift.Id, shift.Status, shift.OriginalId);

    private static void EnsureCopyable(
        Guid root, IReadOnlyDictionary<Guid, List<Shift>> families, AnalyseScenario variant, IReadOnlySet<Guid> allowedGroupIds)
    {
        if (!families.TryGetValue(root, out var family) || VariantShiftFamilyRules.FindSealedOrder(family) is not { } sealedOrder)
        {
            throw new KeyNotFoundException(string.Format(ShiftVisibilityMessages.ShiftNotFound, root));
        }

        if (FindRefusal(family, sealedOrder, variant, allowedGroupIds) is { } refusal)
        {
            throw new InvalidRequestException(refusal);
        }
    }

    private static bool Fits(
        Guid root, IReadOnlyDictionary<Guid, List<Shift>> families, AnalyseScenario variant, IReadOnlySet<Guid> allowedGroupIds)
    {
        return families.TryGetValue(root, out var family)
            && VariantShiftFamilyRules.FindSealedOrder(family) is { } sealedOrder
            && FindRefusal(family, sealedOrder, variant, allowedGroupIds) is null;
    }

    private static string? FindRefusal(
        List<Shift> family, Shift sealedOrder, AnalyseScenario variant, IReadOnlySet<Guid> allowedGroupIds)
    {
        if (!VariantShiftFamilyRules.HasOnlyAllowedGroups(family, allowedGroupIds))
        {
            return GroupsOutsideMessage;
        }

        if (!VariantShiftFamilyRules.HasCustomer(family))
        {
            return VariantShiftWriteGuard.CustomerRequiredMessage;
        }

        if (!VariantShiftFamilyRules.IntersectsPeriod(sealedOrder, variant.FromDate, variant.UntilDate))
        {
            return OutsidePeriodMessage;
        }

        return VariantShiftFamilyRules.RowsInPeriod(family, variant.FromDate, variant.UntilDate) is null
            ? PartsOutsidePeriodMessage
            : null;
    }

    private async Task<Dictionary<Guid, ShiftRef>> LoadRefsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var idList = ids.ToList();

        return await _context.Shift
            .AsNoTracking()
            .Where(shift => !shift.IsDeleted && idList.Contains(shift.Id))
            .Select(shift => new ShiftRef(
                shift.Id,
                shift.Status,
                shift.OriginalId,
                shift.AnalyseToken == null && shift.ScenarioSourceShiftId == null))
            .ToDictionaryAsync(shift => shift.Id, cancellationToken);
    }

    private async Task<Dictionary<Guid, List<Shift>>> LoadFamiliesAsync(IReadOnlyCollection<Guid> roots, CancellationToken cancellationToken)
    {
        if (roots.Count == 0)
        {
            return [];
        }

        var rootList = roots.ToList();

        var rows = await _context.Shift
            .AsNoTracking()
            .AsSplitQuery()
            .Include(shift => shift.GroupItems.Where(item => !item.IsDeleted))
            .Include(shift => shift.ShiftExpenses.Where(expense => !expense.IsDeleted))
            .Include(shift => shift.RequiredQualifications.Where(requirement => !requirement.IsDeleted))
            .Where(shift => !shift.IsDeleted
                && shift.AnalyseToken == null
                && shift.ScenarioSourceShiftId == null
                && (rootList.Contains(shift.Id) || (shift.OriginalId != null && rootList.Contains(shift.OriginalId.Value))))
            .ToListAsync(cancellationToken);

        return rows
            .Where(shift => VariantShiftFamilyRules.FamilyRootOf(shift) is { } root && rootList.Contains(root))
            .GroupBy(shift => VariantShiftFamilyRules.FamilyRootOf(shift)!.Value)
            .ToDictionary(group => group.Key, group => group.ToList());
    }

    private async Task StageAsync(
        IReadOnlyDictionary<Guid, List<Shift>> families, IReadOnlyCollection<Guid> roots, AnalyseScenario variant, CancellationToken cancellationToken)
    {
        var rows = roots
            .SelectMany(root => VariantShiftFamilyRules.RowsInPeriod(families[root], variant.FromDate, variant.UntilDate) ?? [])
            .ToList();
        if (rows.Count == 0)
        {
            return;
        }

        await _context.Shift.AddRangeAsync(VariantShiftFamilyCloner.Clone(rows, variant.Token, variant.FromDate, variant.UntilDate), cancellationToken);
    }

    private sealed record ShiftRef(Guid Id, ShiftStatus Status, Guid? OriginalId, bool IsReal);
}
