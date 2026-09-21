// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF-backed IScheduleActivityProbe. The gate queries are existence checks (AnyAsync) rather than
/// counts, so a group with thousands of assignments costs the same as one with a single row. The two
/// exceptions are CountActiveEmployeesAsync and CountUngroupedPlannableShiftsAsync, whose callers need
/// the size of a set and not merely its existence.
///
/// Group scoping walks the nested set inclusively — Root equal, Lft/Rgt within the group's own
/// bounds — so the group itself and every descendant are covered by ONE query instead of a
/// materialized id list. GroupStaffingLookup uses strict bounds because it asks the opposite
/// question ("is this a proper ancestor of a staffed group"); here the group itself must count.
///
/// Scenario rows are excluded on BOTH sides of the join: a what-if clone sets AnalyseToken on the
/// Work/Shift row AND on the GroupItem that attaches it, so filtering only the entity would let a
/// scenario satisfy a gate that is asking about the real schedule.
///
/// The two prerequisite probes (customers, groups) are deliberately FLAT existence checks with no
/// nested-set condition: 364 of the 438 groups in the reference installation carry Root = NULL with
/// Lft = Rgt = 0, and a scoped query would report those as absent. Here the question is only whether
/// anything exists at all, so scoping would be wrong as well as dangerous.
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
        var hasCustomers = await _context.Client
            .AnyAsync(client => !client.IsDeleted && client.Type == EntityTypeEnum.Customer, cancellationToken);
        var hasGroups = await _context.Group
            .AnyAsync(group => !group.IsDeleted, cancellationToken);

        return new ScheduleSetupState(hasOrders, hasShifts, hasWork, hasCustomers, hasGroups);
    }

    /// <summary>
    /// The membership window is spelled exactly as ClientCoreDataReadRepository spells it — ValidFrom
    /// on or before the reference day, ValidUntil unset or on/after it — so "active person" means the
    /// same thing in both scans. A client without any membership row is not counted: there is no day
    /// on which such a record is employed.
    /// </summary>
    public async Task<int> CountActiveEmployeesAsync(
        DateOnly referenceDate,
        CancellationToken cancellationToken = default)
    {
        var reference = referenceDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        return await _context.Client
            .Where(client => !client.IsDeleted && client.Type == EntityTypeEnum.Employee)
            .Where(client => client.Membership != null
                && client.Membership.ValidFrom <= reference
                && (client.Membership.ValidUntil == null || client.Membership.ValidUntil >= reference))
            .CountAsync(cancellationToken);
    }

    /// <summary>
    /// The anti-join is spelled against GroupItem directly rather than against Shift.GroupItems so the
    /// scenario exclusion can be stated on the membership row as well: a what-if clone sets AnalyseToken
    /// on both sides, and a scenario membership must not make a real duty look grouped. No nested-set
    /// scoping here on purpose - the question is whether ANY group owns the duty, and scoping would
    /// answer a different one. FromDate is deliberately not part of the window: a duty that starts in
    /// the future is planned and therefore concerned, while one whose UntilDate has passed is history.
    /// </summary>
    public async Task<int> CountUngroupedPlannableShiftsAsync(
        DateOnly referenceDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.Shift
            .Where(shift => !shift.IsDeleted && shift.AnalyseToken == null && shift.ScenarioSourceShiftId == null)
            .Where(shift => ShiftStatuses.Contains(shift.Status))
            .Where(shift => shift.ShiftType == ShiftType.IsTask)
            .Where(shift => shift.UntilDate == null || shift.UntilDate >= referenceDate)
            .Where(shift => !_context.GroupItem.Any(item =>
                item.ShiftId == shift.Id && !item.IsDeleted && item.AnalyseToken == null))
            .CountAsync(cancellationToken);
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
