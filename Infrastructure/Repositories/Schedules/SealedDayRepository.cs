// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

/// <summary>
/// EF Core backed implementation of ISealedDayRepository. A group-scoped SealedDay is stored per day and group,
/// independent of entries. It locks a client on that day when the client worked a shift of the group that day,
/// or when the client is an active member of the group on that day (GroupMembershipWindowLoader, the same rule
/// GroupBreakScope uses for sealing and exporting absences) - so a sealed day stays locked for its members even
/// while it is empty.
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
            .Where(s => s.Date >= from && s.Date <= to && s.Level == WorkLockLevel.Closed);

        if (groupId.HasValue)
        {
            query = query.Where(s => s.GroupId == null || s.GroupId == groupId.Value);
        }

        return await query.OrderBy(s => s.Date).ToListAsync(cancellationToken);
    }

    public async Task<int> SoftDeleteRangeAsync(DateOnly from, DateOnly to, Guid? groupId, string deletedBy, CancellationToken cancellationToken = default)
    {
        var rows = await _context.SealedDay
            .Where(s => s.Date >= from && s.Date <= to && s.GroupId == groupId && s.Level == WorkLockLevel.Closed)
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
        var locked = await GetLockedPairsAsync([(date, clientId)], cancellationToken);
        return locked.Count > 0;
    }

    public async Task<bool> IsDayLockedForShiftAsync(DateOnly date, Guid shiftId, CancellationToken cancellationToken = default)
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
            .AnyAsync(s => _context.GroupItem.Any(gi => !gi.IsDeleted
                && gi.AnalyseToken == null
                && gi.ScenarioSourceGroupItemId == null
                && gi.ShiftId == shiftId
                && gi.GroupId == s.GroupId), cancellationToken);
    }

    public async Task<HashSet<(DateOnly Date, Guid ClientId)>> GetLockedPairsAsync(
        IReadOnlyCollection<(DateOnly Date, Guid ClientId)> pairs,
        CancellationToken cancellationToken = default)
    {
        var locking = await DayLockAttribution.LoadLockingSealsAsync(_context, pairs, null, cancellationToken);
        return locking.Keys.ToHashSet();
    }

    public async Task<DateOnly?> FindFirstLockedDateForClientAsync(DateOnly from, DateOnly to, Guid clientId, CancellationToken cancellationToken = default)
    {
        var sealedDates = await _context.SealedDay
            .AsNoTracking()
            .Where(s => s.Date >= from && s.Date <= to)
            .Select(s => s.Date)
            .Distinct()
            .ToListAsync(cancellationToken);

        var locked = await GetLockedPairsAsync(sealedDates.Select(d => (d, clientId)).ToList(), cancellationToken);
        return locked.Count > 0 ? locked.Min(p => p.Date) : (DateOnly?)null;
    }

    public async Task<List<SealedDay>> GetDayApprovalsAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        return await _context.SealedDay
            .AsNoTracking()
            .Where(s => s.Date == date && s.Level == WorkLockLevel.Approved)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> SoftDeleteDayApprovalAsync(DateOnly date, Guid groupId, string deletedBy, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.SealedDay
            .Where(s => s.Date == date && s.GroupId == groupId && s.Level == WorkLockLevel.Approved)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.IsDeleted, true)
                .SetProperty(r => r.DeletedTime, now)
                .SetProperty(r => r.CurrentUserDeleted, deletedBy), cancellationToken);
    }

    public Task<HashSet<(Guid ClientId, DateOnly Date)>> GetLockedClientDaysAsync(
        IReadOnlyCollection<Guid> clientIds,
        DateOnly from,
        DateOnly until,
        CancellationToken cancellationToken = default)
    {
        return DayLockAttribution.LoadLockedClientDaysAsync(_context, clientIds, from, until, cancellationToken);
    }
}
