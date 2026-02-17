using Klacks.Api.Application.Interfaces;
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

    public async Task<IReadOnlyList<SkillUsageRecord>> GetRecordsAsync(DateTime fromDate, CancellationToken cancellationToken = default)
    {
        return await _context.SkillUsageRecords
            .Where(r => r.Timestamp >= fromDate)
            .OrderByDescending(r => r.Timestamp)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SkillUsageRecord>> GetRecordsBySkillAsync(string skillName, DateTime fromDate, CancellationToken cancellationToken = default)
    {
        return await _context.SkillUsageRecords
            .Where(r => r.SkillName == skillName && r.Timestamp >= fromDate)
            .OrderByDescending(r => r.Timestamp)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SkillUsageRecord>> GetRecordsByUserAsync(Guid userId, DateTime fromDate, CancellationToken cancellationToken = default)
    {
        return await _context.SkillUsageRecords
            .Where(r => r.UserId == userId && r.Timestamp >= fromDate)
            .OrderByDescending(r => r.Timestamp)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalExecutionsAsync(DateTime fromDate, CancellationToken cancellationToken = default)
    {
        return await _context.SkillUsageRecords
            .Where(r => r.Timestamp >= fromDate)
            .CountAsync(cancellationToken);
    }

    public async Task<decimal> GetSuccessRateAsync(DateTime fromDate, CancellationToken cancellationToken = default)
    {
        var total = await _context.SkillUsageRecords
            .Where(r => r.Timestamp >= fromDate)
            .CountAsync(cancellationToken);

        if (total == 0)
            return 100m;

        var successful = await _context.SkillUsageRecords
            .Where(r => r.Timestamp >= fromDate && r.Success)
            .CountAsync(cancellationToken);

        return (decimal)successful / total * 100;
    }
}
