// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

public class BreakRepository : BaseRepository<Break>, IBreakRepository
{
    private readonly DataBaseContext _context;

    public BreakRepository(
        DataBaseContext context,
        ILogger<Break> logger)
        : base(context, logger)
    {
        _context = context;
    }

    public async Task<int> SealByDayAndGroup(DateOnly date, Guid groupId, WorkLockLevel level, string sealedBy, CancellationToken cancellationToken = default)
    {
        return await _context.Break
            .Where(b => !b.IsDeleted && b.CurrentDate == date && b.LockLevel < level)
            .Where(b => _context.Work.Any(w => !w.IsDeleted && w.ClientId == b.ClientId && w.CurrentDate == date
                && _context.GroupItem.Any(gi => gi.ShiftId == w.ShiftId && gi.GroupId == groupId && !gi.IsDeleted)))
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.LockLevel, level)
                .SetProperty(b => b.SealedAt, DateTime.UtcNow)
                .SetProperty(b => b.SealedBy, sealedBy), cancellationToken);
    }

    public async Task<int> UnsealByDayAndGroup(DateOnly date, Guid groupId, WorkLockLevel level, CancellationToken cancellationToken = default)
    {
        return await _context.Break
            .Where(b => !b.IsDeleted && b.CurrentDate == date && b.LockLevel == level)
            .Where(b => _context.Work.Any(w => !w.IsDeleted && w.ClientId == b.ClientId && w.CurrentDate == date
                && _context.GroupItem.Any(gi => gi.ShiftId == w.ShiftId && gi.GroupId == groupId && !gi.IsDeleted)))
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.LockLevel, WorkLockLevel.None)
                .SetProperty(b => b.SealedAt, (DateTime?)null)
                .SetProperty(b => b.SealedBy, (string?)null), cancellationToken);
    }

    public async Task<int> SealByPeriod(DateOnly startDate, DateOnly endDate, WorkLockLevel level, string sealedBy, CancellationToken cancellationToken = default)
    {
        return await _context.Break
            .Where(b => !b.IsDeleted && b.CurrentDate >= startDate && b.CurrentDate <= endDate && b.LockLevel < level)
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.LockLevel, level)
                .SetProperty(b => b.SealedAt, DateTime.UtcNow)
                .SetProperty(b => b.SealedBy, sealedBy), cancellationToken);
    }

    public async Task<int> UnsealByPeriod(DateOnly startDate, DateOnly endDate, WorkLockLevel level, CancellationToken cancellationToken = default)
    {
        return await _context.Break
            .Where(b => !b.IsDeleted && b.CurrentDate >= startDate && b.CurrentDate <= endDate && b.LockLevel == level)
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.LockLevel, WorkLockLevel.None)
                .SetProperty(b => b.SealedAt, (DateTime?)null)
                .SetProperty(b => b.SealedBy, (string?)null), cancellationToken);
    }

    public async Task<int> SealByPeriodAndGroup(DateOnly startDate, DateOnly endDate, Guid groupId, WorkLockLevel level, string sealedBy, CancellationToken cancellationToken = default)
    {
        return await _context.Break
            .Where(b => !b.IsDeleted && b.CurrentDate >= startDate && b.CurrentDate <= endDate && b.LockLevel < level)
            .Where(b => _context.Work.Any(w => !w.IsDeleted && w.ClientId == b.ClientId && w.CurrentDate == b.CurrentDate
                && _context.GroupItem.Any(gi => gi.ShiftId == w.ShiftId && gi.GroupId == groupId && !gi.IsDeleted)))
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.LockLevel, level)
                .SetProperty(b => b.SealedAt, DateTime.UtcNow)
                .SetProperty(b => b.SealedBy, sealedBy), cancellationToken);
    }

    public async Task<int> UnsealByPeriodAndGroup(DateOnly startDate, DateOnly endDate, Guid groupId, WorkLockLevel level, CancellationToken cancellationToken = default)
    {
        return await _context.Break
            .Where(b => !b.IsDeleted && b.CurrentDate >= startDate && b.CurrentDate <= endDate && b.LockLevel == level)
            .Where(b => _context.Work.Any(w => !w.IsDeleted && w.ClientId == b.ClientId && w.CurrentDate == b.CurrentDate
                && _context.GroupItem.Any(gi => gi.ShiftId == w.ShiftId && gi.GroupId == groupId && !gi.IsDeleted)))
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.LockLevel, WorkLockLevel.None)
                .SetProperty(b => b.SealedAt, (DateTime?)null)
                .SetProperty(b => b.SealedBy, (string?)null), cancellationToken);
    }

    public async Task<List<(DateOnly Date, int Total, int Sealed)>> GetSealingSummaryAsync(DateOnly from, DateOnly to, Guid? groupId, CancellationToken cancellationToken = default)
    {
        var query = _context.Break.AsNoTracking().Where(b => !b.IsDeleted && b.CurrentDate >= from && b.CurrentDate <= to);

        if (groupId.HasValue)
        {
            var gid = groupId.Value;
            query = query.Where(b => _context.Work.Any(w =>
                !w.IsDeleted &&
                w.ClientId == b.ClientId &&
                w.CurrentDate == b.CurrentDate &&
                _context.GroupItem.Any(gi => gi.ShiftId == w.ShiftId && gi.GroupId == gid && !gi.IsDeleted)));
        }

        var grouped = await query
            .GroupBy(b => b.CurrentDate)
            .Select(g => new
            {
                Date = g.Key,
                Total = g.Count(),
                Sealed = g.Count(b => b.LockLevel == WorkLockLevel.Closed)
            })
            .ToListAsync(cancellationToken);

        return grouped.Select(x => (x.Date, x.Total, x.Sealed)).ToList();
    }
}
