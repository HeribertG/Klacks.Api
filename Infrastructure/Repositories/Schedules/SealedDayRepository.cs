// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

/// <summary>
/// EF Core backed implementation of ISealedDayRepository.
/// </summary>
/// <param name="context">Shared application DbContext</param>
public class SealedDayRepository : ISealedDayRepository
{
    private readonly DataBaseContext _context;

    public SealedDayRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task AddAsync(SealedDay entry, CancellationToken cancellationToken = default)
    {
        entry.Id = entry.Id == Guid.Empty ? Guid.NewGuid() : entry.Id;
        await _context.SealedDay.AddAsync(entry, cancellationToken);
    }

    public async Task<List<SealedDay>> GetRangeAsync(DateOnly from, DateOnly to, Guid? groupId, CancellationToken cancellationToken = default)
    {
        var query = _context.SealedDay
            .AsNoTracking()
            .Where(s => s.Date >= from && s.Date <= to);

        if (groupId.HasValue)
        {
            query = query.Where(s => s.GroupId == null || s.GroupId == groupId.Value);
        }

        return await query.OrderBy(s => s.Date).ToListAsync(cancellationToken);
    }

    public async Task<int> SoftDeleteRangeAsync(DateOnly from, DateOnly to, Guid? groupId, string deletedBy, CancellationToken cancellationToken = default)
    {
        var rows = await _context.SealedDay
            .Where(s => s.Date >= from && s.Date <= to && s.GroupId == groupId)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var row in rows)
        {
            row.IsDeleted = true;
            row.DeletedTime = now;
            row.CurrentUserDeleted = deletedBy;
        }

        return rows.Count;
    }

    public async Task<bool> IsDayLockedAsync(DateOnly date, Guid clientId, CancellationToken cancellationToken = default)
    {
        var globalLocked = await _context.SealedDay
            .AsNoTracking()
            .AnyAsync(s => s.Date == date && s.GroupId == null, cancellationToken);

        if (globalLocked)
        {
            return true;
        }

        return await _context.SealedDay
            .AsNoTracking()
            .Where(s => s.Date == date && s.GroupId != null)
            .AnyAsync(s => _context.Work.Any(w => !w.IsDeleted
                && w.AnalyseToken == null
                && w.ClientId == clientId
                && w.CurrentDate == date
                && _context.GroupItem.Any(gi => !gi.IsDeleted
                    && gi.ShiftId == w.ShiftId
                    && gi.GroupId == s.GroupId)), cancellationToken);
    }
}
