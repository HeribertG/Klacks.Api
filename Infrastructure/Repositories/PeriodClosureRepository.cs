using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories;

public class PeriodClosureRepository : BaseRepository<PeriodClosure>, IPeriodClosureRepository
{
    private readonly DataBaseContext _context;

    public PeriodClosureRepository(DataBaseContext context, ILogger<PeriodClosure> logger)
        : base(context, logger)
    {
        _context = context;
    }

    public async Task<PeriodClosure?> GetOverlapping(DateOnly date)
    {
        return await _context.PeriodClosures
            .FirstOrDefaultAsync(pc => pc.StartDate <= date && pc.EndDate >= date);
    }

    public async Task<List<PeriodClosure>> GetByDateRange(DateOnly startDate, DateOnly endDate)
    {
        return await _context.PeriodClosures
            .Where(pc => pc.StartDate <= endDate && pc.EndDate >= startDate)
            .OrderBy(pc => pc.StartDate)
            .ToListAsync();
    }

    public async Task<bool> ExistsForDate(DateOnly date)
    {
        return await _context.PeriodClosures
            .AnyAsync(pc => pc.StartDate <= date && pc.EndDate >= date);
    }
}
