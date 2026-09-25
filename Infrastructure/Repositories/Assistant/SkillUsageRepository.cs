// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class SkillUsageRepository : ISkillUsageRepository
{
    private readonly DataBaseContext _context;

    public SkillUsageRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task AddAsync(SkillUsageRecord record, CancellationToken cancellationToken = default)
    {
        await _context.SkillUsageRecords.AddAsync(record, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    // Every statistic below counts calls that ran; a row the stop of its turn cancelled is none.
    internal IQueryable<SkillUsageRecord> RanRows() =>
        _context.SkillUsageRecords.Where(SkillUsagePredicates.Ran);

    public async Task<IReadOnlyList<SkillUsageRecord>> GetRecordsAsync(DateTime fromDate, CancellationToken cancellationToken = default)
    {
        return await RanRows()
            .Where(r => r.Timestamp >= fromDate)
            .OrderByDescending(r => r.Timestamp)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SkillUsageRecord>> GetRecordsBySkillAsync(string skillName, DateTime fromDate, CancellationToken cancellationToken = default)
    {
        return await RanRows()
            .Where(r => r.SkillName == skillName && r.Timestamp >= fromDate)
            .OrderByDescending(r => r.Timestamp)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SkillUsageRecord>> GetRecordsByUserAsync(Guid userId, DateTime fromDate, CancellationToken cancellationToken = default)
    {
        return await RanRows()
            .Where(r => r.UserId == userId && r.Timestamp >= fromDate)
            .OrderByDescending(r => r.Timestamp)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalExecutionsAsync(DateTime fromDate, CancellationToken cancellationToken = default)
    {
        return await RanRows()
            .Where(r => r.Timestamp >= fromDate)
            .CountAsync(cancellationToken);
    }

    public async Task<decimal> GetSuccessRateAsync(DateTime fromDate, CancellationToken cancellationToken = default)
    {
        var total = await RanRows()
            .Where(r => r.Timestamp >= fromDate)
            .CountAsync(cancellationToken);

        if (total == 0)
            return 100m;

        var successful = await RanRows()
            .Where(r => r.Timestamp >= fromDate && r.Success)
            .CountAsync(cancellationToken);

        return (decimal)successful / total * 100;
    }

    public async Task<IReadOnlyList<SkillUsageRecord>> GetByTurnIdAsync(Guid turnId, CancellationToken cancellationToken = default)
    {
        return await _context.SkillUsageRecords
            .Where(r => r.TurnId == turnId)
            .OrderBy(r => r.Timestamp)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<SkillUsageRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SkillUsageRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task UpdateAsync(SkillUsageRecord record, CancellationToken cancellationToken = default)
    {
        _context.SkillUsageRecords.Update(record);
        await _context.SaveChangesAsync(cancellationToken);
    }

    internal IQueryable<SkillUsageRecord> DispatchedRowsOfTurn(Guid turnId) =>
        _context.SkillUsageRecords.Where(r => r.TurnId == turnId && r.UiActionStatus == UiActionStatus.Dispatched);

    public async Task<int> CancelDispatchedForTurnAsync(Guid turnId, CancellationToken cancellationToken = default)
    {
        var rows = await DispatchedRowsOfTurn(turnId).ToListAsync(cancellationToken);
        if (rows.Count == 0)
        {
            return 0;
        }

        var now = DateTime.UtcNow;
        foreach (var row in rows)
        {
            row.UiActionStatus = UiActionStatus.Cancelled;
            row.Success = false;
            row.UpdateTime = now;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return rows.Count;
    }
}
