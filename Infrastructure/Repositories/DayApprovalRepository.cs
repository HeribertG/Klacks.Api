using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories;

public class DayApprovalRepository : BaseRepository<DayApproval>, IDayApprovalRepository
{
    private readonly DataBaseContext _context;

    public DayApprovalRepository(DataBaseContext context, ILogger<DayApproval> logger)
        : base(context, logger)
    {
        _context = context;
    }

    public async Task<DayApproval?> GetByDateAndGroup(DateOnly date, Guid groupId)
    {
        return await _context.DayApprovals
            .FirstOrDefaultAsync(da => da.ApprovalDate == date && da.GroupId == groupId);
    }

    public async Task<List<DayApproval>> GetByDateRange(DateOnly startDate, DateOnly endDate, Guid? groupId = null)
    {
        var query = _context.DayApprovals
            .Where(da => da.ApprovalDate >= startDate && da.ApprovalDate <= endDate);

        if (groupId.HasValue)
        {
            query = query.Where(da => da.GroupId == groupId.Value);
        }

        return await query.ToListAsync();
    }

    public async Task<bool> ExistsForDateAndGroup(DateOnly date, Guid groupId)
    {
        return await _context.DayApprovals
            .AnyAsync(da => da.ApprovalDate == date && da.GroupId == groupId);
    }
}
