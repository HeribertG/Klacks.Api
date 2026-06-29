// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Read-side repository feeding the resource-monitor dashboard card with contracts, clients,
/// shifts, absences and scheduling settings for a given year (optionally scoped to a group).
/// </summary>
using Klacks.Api.Application.DTOs.Dashboard;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Dashboard;

public class ResourceMonitorReadRepository : IResourceMonitorReadRepository
{
    private readonly DataBaseContext _context;

    public ResourceMonitorReadRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<HashSet<Guid>> GetGroupShiftIds(Guid groupId, CancellationToken cancellationToken)
    {
        return await _context.GroupItem
            .Where(gi => gi.GroupId == groupId && !gi.IsDeleted && gi.ShiftId != null)
            .Select(gi => gi.ShiftId!.Value)
            .ToHashSetAsync(cancellationToken);
    }

    public async Task<HashSet<Guid>> GetClientIdsForShiftsInRange(
        IReadOnlyCollection<Guid> shiftIds,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken)
    {
        return await _context.Work
            .Where(w => !w.IsDeleted
                && w.CurrentDate >= startDate
                && w.CurrentDate <= endDate
                && w.AnalyseToken == null
                && shiftIds.Contains(w.ShiftId))
            .Select(w => w.ClientId)
            .Distinct()
            .ToHashSetAsync(cancellationToken);
    }

    public async Task<List<ClientContract>> GetActiveContracts(
        DateOnly startDate,
        DateOnly endDate,
        IReadOnlyCollection<Guid>? clientIds,
        CancellationToken cancellationToken)
    {
        var query = _context.ClientContract
            .Include(cc => cc.Contract).ThenInclude(c => c!.SchedulingRule)
            .Where(cc => !cc.IsDeleted
                && cc.IsActive
                && cc.FromDate <= endDate
                && (cc.UntilDate == null || cc.UntilDate >= startDate));

        if (clientIds != null)
        {
            query = query.Where(cc => clientIds.Contains(cc.ClientId));
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<HashSet<Guid>> GetEmployeeClientIds(IReadOnlyCollection<Guid>? clientIds, CancellationToken cancellationToken)
    {
        var query = _context.Client
            .Where(c => c.Type != EntityTypeEnum.Customer);

        if (clientIds != null)
        {
            query = query.Where(c => clientIds.Contains(c.Id));
        }

        return await query
            .Select(c => c.Id)
            .ToHashSetAsync(cancellationToken);
    }

    public async Task<HashSet<Guid>> GetContainedShiftIds(CancellationToken cancellationToken)
    {
        return await _context.ContainerTemplateItem
            .Where(cti => !cti.IsDeleted
                && cti.ShiftId != null
                && !cti.ContainerTemplate.IsDeleted)
            .Select(cti => cti.ShiftId!.Value)
            .Distinct()
            .ToHashSetAsync(cancellationToken);
    }

    public async Task<List<DashboardShiftRow>> GetActiveShifts(
        DateOnly startDate,
        DateOnly endDate,
        IReadOnlyCollection<Guid>? shiftIds,
        IReadOnlyCollection<Guid> containedShiftIds,
        CancellationToken cancellationToken)
    {
        var query = _context.Shift
            .Where(s => !s.IsDeleted
                && !s.IsTimeRange
                && !s.IsSporadic
                && s.AnalyseToken == null
                && s.Status != ShiftStatus.SealedOrder
                && !containedShiftIds.Contains(s.Id)
                && s.FromDate <= endDate
                && (s.UntilDate == null || s.UntilDate >= startDate));

        if (shiftIds != null)
        {
            query = query.Where(s => shiftIds.Contains(s.Id));
        }

        var rows = await query
            .Select(s => new
            {
                s.FromDate,
                s.UntilDate,
                s.IsMonday,
                s.IsTuesday,
                s.IsWednesday,
                s.IsThursday,
                s.IsFriday,
                s.IsSaturday,
                s.IsSunday,
            })
            .ToListAsync(cancellationToken);

        return rows.Select(s => new DashboardShiftRow(
            s.FromDate,
            s.UntilDate,
            s.IsMonday,
            s.IsTuesday,
            s.IsWednesday,
            s.IsThursday,
            s.IsFriday,
            s.IsSaturday,
            s.IsSunday)).ToList();
    }

    public async Task<List<DashboardAbsenceRow>> GetAbsences(
        DateTime periodStart,
        DateTime periodEnd,
        IReadOnlyCollection<Guid>? clientIds,
        CancellationToken cancellationToken)
    {
        var query = _context.BreakPlaceholder
            .Include(bp => bp.Absence)
            .Where(bp => !bp.IsDeleted
                && bp.From < periodEnd
                && bp.Until > periodStart);

        if (clientIds != null)
        {
            query = query.Where(bp => clientIds.Contains(bp.ClientId));
        }

        var rows = await query
            .Select(bp => new { bp.From, bp.Until, DefaultValue = bp.Absence.DefaultValue })
            .ToListAsync(cancellationToken);

        return rows.Select(x => new DashboardAbsenceRow(x.From, x.Until, x.DefaultValue)).ToList();
    }

    public async Task<string?> GetSettingValue(string type, CancellationToken cancellationToken)
    {
        return await _context.Settings
            .Where(s => s.Type == type)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
