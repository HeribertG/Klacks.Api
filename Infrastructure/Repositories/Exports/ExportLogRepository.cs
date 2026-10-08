// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Exports;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Exports;

/// <summary>
/// EF Core backed implementation of IExportLogRepository.
/// </summary>
/// <param name="context">Shared application DbContext</param>
public class ExportLogRepository : IExportLogRepository
{
    private readonly DataBaseContext _context;

    public ExportLogRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task AddAsync(ExportLog entry, CancellationToken cancellationToken = default)
    {
        entry.Id = entry.Id == Guid.Empty ? Guid.NewGuid() : entry.Id;
        await _context.ExportLog.AddAsync(entry, cancellationToken);
    }

    public async Task<ExportLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ExportLog
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);
    }

    public async Task<List<ExportLog>> GetRangeAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        return await _context.ExportLog
            .AsNoTracking()
            .Where(e => !e.IsDeleted && e.StartDate <= to && e.EndDate >= from)
            .OrderByDescending(e => e.ExportedAt)
            .ToListAsync(cancellationToken);
    }
}
