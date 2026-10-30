// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves the group of a split_shifts_by_weekday call and sorts its plannable shifts (status OriginalShift, ShiftType
/// IsTask, real plan, linked directly to the group; shifts the caller may not see are left out and not counted) into the ones to cut and the ones to skip: shifts
/// with planned assignments, sporadic shifts, shifts without a sealed order, orders that already have split parts and shifts
/// that run on fewer than two weekdays. Only reads.
/// </summary>
/// <param name="groupRepository">Lists the groups the name is resolved against</param>
/// <param name="groupVisibilityGuard">Limits the groups to the caller's visibility scope</param>
/// <param name="groupItemRepository">Lists the services linked directly to the group</param>
/// <param name="shiftReader">Scans the plannable shifts and their parts and assignments</param>
/// <param name="shiftVisibilityGuard">Drops shifts a restricted caller may not see</param>

using System.Globalization;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Skills;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

public class ShiftWeekdayCutPlanner : IShiftWeekdayCutPlanner
{
    private const int MinWeekdaysToCut = 2;
    private const string SingleWeekdayReason = "runs on fewer than two weekdays";
    private const string AlreadySplitReason = "the order already has split parts";
    private const string SporadicReason = "is a sporadic shift";
    private const string NoOrderReason = "has no sealed order to cut from";
    private const string HasWorksReason = "already has planned assignments, which would end up on the wrong weekday part";
    private const string UnresolvedGroupTemplate = "The group '{0}' could not be resolved.";

    private readonly BundleGroupFilterResolver _groupFilterResolver;
    private readonly IGroupItemRepository _groupItemRepository;
    private readonly IShiftReader _shiftReader;
    private readonly IShiftVisibilityGuard _shiftVisibilityGuard;

    public ShiftWeekdayCutPlanner(
        IGroupRepository groupRepository,
        IGroupVisibilityGuard groupVisibilityGuard,
        IGroupItemRepository groupItemRepository,
        IShiftReader shiftReader,
        IShiftVisibilityGuard shiftVisibilityGuard)
    {
        _groupFilterResolver = new BundleGroupFilterResolver(groupRepository, groupVisibilityGuard, groupItemRepository);
        _groupItemRepository = groupItemRepository;
        _shiftReader = shiftReader;
        _shiftVisibilityGuard = shiftVisibilityGuard;
    }

    public async Task<ShiftWeekdayCutSelection> PlanAsync(string groupName, CancellationToken cancellationToken = default)
    {
        var (groups, groupError) = await _groupFilterResolver.ResolveAsync(null, groupName, cancellationToken);
        if (groups is not { Count: 1 })
        {
            return ShiftWeekdayCutSelection.Refuse(
                groupError ?? string.Format(CultureInfo.InvariantCulture, UnresolvedGroupTemplate, groupName));
        }

        var group = groups[0];
        var groupShiftIds = await _groupItemRepository.GetShiftIdsByGroupIds([group.Id], cancellationToken);

        var plannable = await _shiftReader.ScanRealPlanAsync(
            new RealPlanShiftScan(ShiftStatus.OriginalShift) { ShiftType = ShiftType.IsTask, OnlyIds = groupShiftIds },
            cancellationToken);
        var visibleIds = await _shiftVisibilityGuard.FilterVisibleAsync(plannable.Select(s => s.Id).ToList(), cancellationToken);
        var parts = await _shiftReader.ScanRealPlanAsync(
            new RealPlanShiftScan(ShiftStatus.SplitShift) { ShiftType = ShiftType.IsTask, OnlyIds = groupShiftIds },
            cancellationToken);
        var splitOrderIds = parts.Where(p => p.OriginalId.HasValue).Select(p => p.OriginalId!.Value).ToHashSet();
        var withWorks = await _shiftReader.GetShiftIdsWithWorksAsync(plannable.Select(s => s.Id).ToList(), cancellationToken);

        var toCut = new List<Shift>();
        var skipped = new List<ShiftWeekdayCutSkip>();
        foreach (var shift in plannable.Where(s => visibleIds.Contains(s.Id)))
        {
            var reason = SkipReason(shift, splitOrderIds, withWorks);
            if (reason is null)
            {
                toCut.Add(shift);
            }
            else
            {
                skipped.Add(new ShiftWeekdayCutSkip(shift.Id, shift.Name, reason));
            }
        }

        return new ShiftWeekdayCutSelection(group.Id, group.Name, toCut, skipped, null);
    }

    private static string? SkipReason(Shift shift, HashSet<Guid> splitOrderIds, IReadOnlySet<Guid> withWorks)
    {
        if (withWorks.Contains(shift.Id))
        {
            return HasWorksReason;
        }

        if (shift.IsSporadic)
        {
            return SporadicReason;
        }

        if (shift.OriginalId is null)
        {
            return NoOrderReason;
        }

        if (splitOrderIds.Contains(shift.OriginalId.Value))
        {
            return AlreadySplitReason;
        }

        return ShiftWeekdayCutBuilder.ActiveIsoWeekdays(shift).Count < MinWeekdaysToCut ? SingleWeekdayReason : null;
    }
}
