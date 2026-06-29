// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Read-side repository feeding the shift-coverage statistics card with shift/group assignments,
/// group names and locked work entries for a month.
/// </summary>
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Dashboard;

public class ShiftCoverageReadRepository : IShiftCoverageReadRepository
{
    private readonly DataBaseContext _context;

    public ShiftCoverageReadRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<List<(Guid? ShiftId, Guid GroupId)>> GetShiftGroupAssignments(CancellationToken cancellationToken)
    {
        var rows = await _context.GroupItem
            .Where(gi => gi.ShiftId != null && !gi.IsDeleted)
            .Select(gi => new { gi.ShiftId, gi.GroupId })
            .ToListAsync(cancellationToken);

        return rows.Select(x => (x.ShiftId, x.GroupId)).ToList();
    }

    public async Task<List<(Guid Id, string Name)>> GetActiveGroups(CancellationToken cancellationToken)
    {
        var rows = await _context.Group
            .Where(g => !g.IsDeleted)
            .Select(g => new { g.Id, g.Name })
            .ToListAsync(cancellationToken);

        return rows.Select(x => (x.Id, x.Name)).ToList();
    }

    public async Task<List<(Guid ShiftId, WorkLockLevel LockLevel)>> GetWorkLockEntries(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken)
    {
        var rows = await _context.Work
            .Where(w => !w.IsDeleted
                && w.AnalyseToken == null
                && w.CurrentDate >= startDate
                && w.CurrentDate <= endDate)
            .Select(w => new { w.ShiftId, w.LockLevel })
            .ToListAsync(cancellationToken);

        return rows.Select(x => (x.ShiftId, x.LockLevel)).ToList();
    }
}
