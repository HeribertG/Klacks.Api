// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core repository for export format override rows (one active row per format key).
/// </summary>
/// <param name="context">The database context for the export_format_override table</param>
using Klacks.Api.Application.Interfaces.Exports;
using Klacks.Api.Domain.Models.Exports;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Exports;

public class ExportFormatOverrideRepository : IExportFormatOverrideRepository
{
    private readonly DataBaseContext _context;

    public ExportFormatOverrideRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ExportFormatOverride>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ExportFormatOverride
            .AsNoTracking()
            .OrderBy(o => o.FormatKey)
            .ToListAsync(cancellationToken);
    }

    public async Task<ExportFormatOverride?> GetByFormatKeyAsync(string formatKey, CancellationToken cancellationToken = default)
    {
        return await _context.ExportFormatOverride
            .FirstOrDefaultAsync(o => o.FormatKey == formatKey, cancellationToken);
    }

    public async Task AddAsync(ExportFormatOverride entry, CancellationToken cancellationToken = default)
    {
        entry.Id = entry.Id == Guid.Empty ? Guid.NewGuid() : entry.Id;
        await _context.ExportFormatOverride.AddAsync(entry, cancellationToken);
    }

    public Task DeleteAsync(ExportFormatOverride entry, CancellationToken cancellationToken = default)
    {
        _context.ExportFormatOverride.Remove(entry);
        return Task.CompletedTask;
    }
}
