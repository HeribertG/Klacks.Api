// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Exports;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Exports;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Exports;

/// <summary>
/// EF Core backed implementation of IExportLogItemRepository. Writes are staged only; the caller commits.
/// </summary>
/// <param name="context">Shared application DbContext</param>
public class ExportLogItemRepository : IExportLogItemRepository
{
    private readonly DataBaseContext _context;

    public ExportLogItemRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task AddRangeAsync(IEnumerable<ExportLogItem> items, CancellationToken cancellationToken = default)
    {
        var staged = items.ToList();
        foreach (var item in staged)
        {
            item.Id = item.Id == Guid.Empty ? Guid.NewGuid() : item.Id;
        }

        await _context.ExportLogItem.AddRangeAsync(staged, cancellationToken);
    }

    public async Task<Dictionary<Guid, LatestExportedItem>> GetLatestItemsAsync(
        DateOnly from,
        DateOnly until,
        string format,
        IReadOnlyCollection<Guid>? clientIds,
        CancellationToken cancellationToken = default)
    {
        var rows = await ExactPeriodQuery(from, until, format, clientIds)
            .Select(i => new LatestExportedItem(i.ClientId, i.ContentHash, i.Revision))
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => r.ClientId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.Revision).First());
    }
    public async Task<List<ExportLogItem>> GetOverlappingAsync(
        IReadOnlyCollection<Guid> clientIds,
        DateOnly from,
        DateOnly until,
        CancellationToken cancellationToken = default)
    {
        var ids = clientIds.ToList();

        return await _context.ExportLogItem
            .AsNoTracking()
            .Where(i => !i.IsDeleted
                && ids.Contains(i.ClientId)
                && i.StartDate <= until
                && i.EndDate >= from
                && (i.StartDate != from || i.EndDate != until))
            .OrderBy(i => i.ClientId)
            .ThenBy(i => i.StartDate)
            .ThenBy(i => i.EndDate)
            .ToListAsync(cancellationToken);
    }

    private IQueryable<ExportLogItem> ExactPeriodQuery(
        DateOnly from,
        DateOnly until,
        string format,
        IReadOnlyCollection<Guid>? clientIds)
    {
        var query = _context.ExportLogItem
            .AsNoTracking()
            .Where(i => !i.IsDeleted
                && i.StartDate == from
                && i.EndDate == until
                && i.Format == format);

        if (clientIds is not null)
        {
            var ids = clientIds.ToList();
            query = query.Where(i => ids.Contains(i.ClientId));
        }

        return query;
    }
}
