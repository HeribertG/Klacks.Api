// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF-backed IScheduleActivityProbe. Every query is an existence check (AnyAsync) rather than a
/// count, so a group with thousands of assignments costs the same as one with a single row.
///
/// Group scoping walks the nested set inclusively — Root equal, Lft/Rgt within the group's own
/// bounds — so the group itself and every descendant are covered by ONE query instead of a
/// materialized id list. GroupStaffingLookup uses strict bounds because it asks the opposite
/// question ("is this a proper ancestor of a staffed group"); here the group itself must count.
///
/// Scenario rows are excluded on BOTH sides of the join: a what-if clone sets AnalyseToken on the
/// Work/Shift row AND on the GroupItem that attaches it, so filtering only the entity would let a
/// scenario satisfy a gate that is asking about the real schedule.
/// </summary>
/// <param name="context">EF context the existence checks run against.</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class ScheduleActivityProbe : IScheduleActivityProbe
{
    private static readonly ShiftStatus[] OrderStatuses = [ShiftStatus.OriginalOrder, ShiftStatus.SealedOrder];
    private static readonly ShiftStatus[] ShiftStatuses = [ShiftStatus.OriginalShift, ShiftStatus.SplitShift];

    private readonly DataBaseContext _context;

    public ScheduleActivityProbe(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<bool> HasWorkInRangeAsync(
        Group group,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var groupItems = ScopedGroupItems(group);

        return await _context.Work
            .Where(work => !work.IsDeleted && work.AnalyseToken == null)
            .Where(work => work.CurrentDate >= from && work.CurrentDate <= to)
            .Where(work => groupItems.Any(item => item.ShiftId == work.ShiftId))
            .AnyAsync(cancellationToken);
    }

    public async Task<bool> HasPlannableShiftsInRangeAsync(
        Group group,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var groupItems = ScopedGroupItems(group);

        return await _context.Shift
            .Where(shift => !shift.IsDeleted && shift.AnalyseToken == null && shift.ScenarioSourceShiftId == null)
            .Where(shift => ShiftStatuses.Contains(shift.Status))
            .Where(shift => shift.FromDate <= to && (shift.UntilDate == null || shift.UntilDate >= from))
            .Where(shift => groupItems.Any(item => item.ShiftId == shift.Id))
            .AnyAsync(cancellationToken);
    }

    public async Task<bool> HasAnyWorkInRangeAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        return await _context.Work
            .Where(work => !work.IsDeleted && work.AnalyseToken == null)
            .Where(work => work.CurrentDate >= from && work.CurrentDate <= to)
            .AnyAsync(cancellationToken);
    }

    public async Task<ScheduleSetupState> GetSetupStateAsync(CancellationToken cancellationToken = default)
    {
        var realShifts = _context.Shift
            .Where(shift => !shift.IsDeleted && shift.AnalyseToken == null && shift.ScenarioSourceShiftId == null);

        var hasOrders = await realShifts.AnyAsync(shift => OrderStatuses.Contains(shift.Status), cancellationToken);
        var hasShifts = await realShifts.AnyAsync(shift => ShiftStatuses.Contains(shift.Status), cancellationToken);
        var hasWork = await _context.Work
            .AnyAsync(work => !work.IsDeleted && work.AnalyseToken == null, cancellationToken);

        return new ScheduleSetupState(hasOrders, hasShifts, hasWork);
    }

    /// <summary>
    /// The group's own GroupItem rows plus those of its descendants. The nested-set branch is guarded
    /// by a Root null check because 364 of the 438 groups in the reference installation carry
    /// Root = NULL with Lft = Rgt = 0: SQL evaluates NULL = NULL as NULL rather than true, so without
    /// the explicit self match those groups would resolve to an empty scope and every gate built on
    /// this probe would silently suppress their events.
    /// </summary>
    private IQueryable<GroupItem> ScopedGroupItems(Group group) =>
        _context.GroupItem
            .Where(item => !item.IsDeleted && item.ShiftId != null)
            .Where(item => item.AnalyseToken == null && item.ScenarioSourceGroupItemId == null)
            .Where(item => item.GroupId == group.Id
                || _context.Group.Any(scoped =>
                    scoped.Id == item.GroupId
                    && group.Root != null
                    && scoped.Root == group.Root
                    && scoped.Lft >= group.Lft
                    && scoped.Rgt <= group.Rgt));
}
