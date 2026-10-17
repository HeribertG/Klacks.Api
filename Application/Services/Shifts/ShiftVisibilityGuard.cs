// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Group-visibility check for shift writes. Uses the restriction of the shift reads
/// (IShiftGroupFilterService.ResolveShiftListGroupIdsAsync: null = unrestricted, including "no calling user") and the
/// shared ShiftGroupVisibilityFilter rule, so reads and writes cannot drift apart. Group assignments are checked through
/// IGroupVisibilityGuard, which applies the same "no user = unrestricted" rule.
/// </summary>
/// <param name="shiftGroupFilterService">Resolves the caller's flat group restriction</param>
/// <param name="shiftVisibilityReader">Answers which shift ids exist and are visible, and reads stored group items</param>
/// <param name="groupVisibilityGuard">Decides whether a group whose assignment changes is visible</param>
/// <param name="logger">Logs refused writes</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Shifts;
using Klacks.Api.Domain.Services.ShiftSchedule;

namespace Klacks.Api.Application.Services.Shifts;

public class ShiftVisibilityGuard : IShiftVisibilityGuard
{
    private readonly IShiftGroupFilterService _shiftGroupFilterService;
    private readonly IShiftVisibilityReader _shiftVisibilityReader;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;
    private readonly ILogger<ShiftVisibilityGuard> _logger;

    public ShiftVisibilityGuard(
        IShiftGroupFilterService shiftGroupFilterService,
        IShiftVisibilityReader shiftVisibilityReader,
        IGroupVisibilityGuard groupVisibilityGuard,
        ILogger<ShiftVisibilityGuard> logger)
    {
        _shiftGroupFilterService = shiftGroupFilterService;
        _shiftVisibilityReader = shiftVisibilityReader;
        _groupVisibilityGuard = groupVisibilityGuard;
        _logger = logger;
    }

    public async Task<bool> IsVisibleAsync(Guid shiftId, CancellationToken cancellationToken = default)
    {
        return await FindFirstHiddenAsync([shiftId], cancellationToken) == null;
    }

    public async Task<bool> IsVisibleAsync(Shift shift, CancellationToken cancellationToken = default)
    {
        var visibleGroupIds = await _shiftGroupFilterService.ResolveShiftListGroupIdsAsync(null);
        return ShiftGroupVisibilityFilter.IsVisible(shift, visibleGroupIds);
    }

    public async Task<Guid?> FindFirstHiddenAsync(IReadOnlyCollection<Guid> shiftIds, CancellationToken cancellationToken = default)
    {
        var distinctIds = shiftIds.Distinct().ToList();
        if (distinctIds.Count == 0)
        {
            return null;
        }

        var visibleIds = await FilterVisibleAsync(distinctIds, cancellationToken);
        foreach (var shiftId in distinctIds)
        {
            if (!visibleIds.Contains(shiftId))
            {
                _logger.LogWarning("Group-restricted caller refused write on hidden or missing shift {ShiftId}", shiftId);
                return shiftId;
            }
        }

        return null;
    }

    public async Task<IReadOnlySet<Guid>> FilterVisibleAsync(IReadOnlyCollection<Guid> shiftIds, CancellationToken cancellationToken = default)
    {
        var distinctIds = shiftIds.Distinct().ToList();
        if (distinctIds.Count == 0)
        {
            return new HashSet<Guid>();
        }

        var visibleGroupIds = await _shiftGroupFilterService.ResolveShiftListGroupIdsAsync(null);
        if (visibleGroupIds == null)
        {
            return distinctIds.ToHashSet();
        }

        return (await _shiftVisibilityReader.FilterVisibleShiftIdsAsync(distinctIds, visibleGroupIds, cancellationToken))
            .ToHashSet();
    }

    public async Task EnsureGroupChangesVisibleAsync(
        IReadOnlyCollection<GroupItem> stored, IReadOnlyCollection<GroupItem> requested, CancellationToken cancellationToken = default)
    {
        var changedGroupIds = ShiftGroupAssignmentChanges.ChangedGroupIds(stored, requested);
        if (changedGroupIds.Count == 0
            || await _groupVisibilityGuard.AreAllGroupsVisibleAsync(changedGroupIds, cancellationToken))
        {
            return;
        }

        foreach (var groupId in changedGroupIds)
        {
            if (!await _groupVisibilityGuard.IsGroupVisibleAsync(groupId, cancellationToken))
            {
                _logger.LogWarning("Group-restricted caller refused shift group change on hidden group {GroupId}", groupId);
                throw new KeyNotFoundException(string.Format(ShiftVisibilityMessages.GroupNotFound, groupId));
            }
        }
    }

    public async Task EnsureGroupChangesVisibleAsync(
        Guid shiftId, IReadOnlyCollection<GroupItem> requested, CancellationToken cancellationToken = default)
    {
        if (await _groupVisibilityGuard.IsUnrestrictedAsync(cancellationToken))
        {
            return;
        }

        var stored = await _shiftVisibilityReader.GetActiveGroupItemsAsync(shiftId, cancellationToken);
        await EnsureGroupChangesVisibleAsync(stored, requested, cancellationToken);
    }
}
