// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

/// <summary>
/// EF Core backed implementation of IPeriodAuditLogRepository.
/// </summary>
/// <param name="context">Shared application DbContext</param>
public class PeriodAuditLogRepository : IPeriodAuditLogRepository
{
    private readonly DataBaseContext _context;

    public PeriodAuditLogRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task AddAsync(PeriodAuditLog entry, CancellationToken cancellationToken = default)
    {
        entry.Id = entry.Id == Guid.Empty ? Guid.NewGuid() : entry.Id;
        await _context.PeriodAuditLog.AddAsync(entry, cancellationToken);
    }

    public async Task<List<PeriodAuditLog>> GetRangeAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        return await _context.PeriodAuditLog
            .AsNoTracking()
            .Where(e => !e.IsDeleted && e.StartDate <= to && e.EndDate >= from)
            .OrderByDescending(e => e.PerformedAt)
            .ToListAsync(cancellationToken);
    }
}
