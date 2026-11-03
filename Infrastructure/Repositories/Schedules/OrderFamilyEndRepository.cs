// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads and locks a real shift family for setting its end date. A family row is the head row or is found through OriginalId
/// (ShiftFamilyRowsRule), and carries neither an analyse token nor a scenario source. The lock is a row lock of PostgreSQL; providers other than Npgsql (the
/// in-memory test store) have none and do nothing.
/// </summary>
/// <param name="context">The scoped context whose current transaction owns the lock</param>

using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

public class OrderFamilyEndRepository : IOrderFamilyEndRepository
{
    private readonly DataBaseContext _context;

    public OrderFamilyEndRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task LockHeadAsync(Guid headId, CancellationToken cancellationToken = default)
    {
        if (!_context.Database.IsNpgsql())
        {
            return;
        }

        await _context.Database.ExecuteSqlAsync($"SELECT id FROM shift WHERE id = {headId} FOR UPDATE", cancellationToken);
    }

    public Task<List<Shift>> GetTrackedFamilyAsync(Guid headId, CancellationToken cancellationToken = default)
        => FamilyRows(headId).ToListAsync(cancellationToken);

    public async Task<DateOnly?> FindLastWorkDateAfterAsync(Guid headId, DateOnly afterDate, CancellationToken cancellationToken = default)
    {
        var shiftIdsInTree = FamilyRows(headId).Select(s => s.Id);

        return await _context.Work
            .AsNoTracking()
            .Where(w => !w.IsDeleted && w.AnalyseToken == null && shiftIdsInTree.Contains(w.ShiftId) && w.CurrentDate > afterDate)
            .MaxAsync(w => (DateOnly?)w.CurrentDate, cancellationToken);
    }

    public async Task<DateOnly?> FindLastScenarioWorkDateAfterAsync(Guid headId, DateOnly afterDate, CancellationToken cancellationToken = default)
    {
        var familyIds = FamilyRows(headId).Select(s => s.Id);
        var cloneIds = _context.Shift
            .IgnoreQueryFilters()
            .Where(s => !s.IsDeleted && s.ScenarioSourceShiftId.HasValue && familyIds.Contains(s.ScenarioSourceShiftId.Value))
            .Select(s => s.Id);

        return await _context.Work
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(w => !w.IsDeleted && w.AnalyseToken != null && cloneIds.Contains(w.ShiftId) && w.CurrentDate > afterDate)
            .MaxAsync(w => (DateOnly?)w.CurrentDate, cancellationToken);
    }

    public async Task<IReadOnlySet<Guid>> FindShiftIdsWithRealWorkAsync(IReadOnlyList<Guid> shiftIds, CancellationToken cancellationToken = default)
    {
        var ids = await _context.Work
            .AsNoTracking()
            .Where(w => !w.IsDeleted && w.AnalyseToken == null && shiftIds.Contains(w.ShiftId))
            .Select(w => w.ShiftId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return ids.ToHashSet();
    }

    public Task<OrderFamilyUsage> GetUsageAsync(IReadOnlyList<Guid> shiftIds, CancellationToken cancellationToken = default)
        => OrderFamilyUsageQuery.ReadAsync(_context, shiftIds, cancellationToken);

    private IQueryable<Shift> FamilyRows(Guid headId)
        => _context.Shift
            .Where(ShiftFamilyRowsRule.Of(headId))
            .Where(s => s.AnalyseToken == null && s.ScenarioSourceShiftId == null);
}
