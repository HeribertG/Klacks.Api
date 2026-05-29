// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core repository for UpdateHistory rows (audit log + hand-off queue).
/// </summary>
/// <param name="context">The database context for the update_history table</param>
using Klacks.Api.Domain.Interfaces.Update;
using Klacks.Api.Domain.Models.Update;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Update;

public class UpdateHistoryRepository : IUpdateHistoryRepository
{
    private static readonly UpdateOperationStatus[] ActiveStatuses =
        [UpdateOperationStatus.Pending, UpdateOperationStatus.Running];

    private readonly DataBaseContext _context;

    public UpdateHistoryRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<UpdateHistory> AddAsync(UpdateHistory entry, CancellationToken cancellationToken = default)
    {
        await _context.UpdateHistory.AddAsync(entry, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entry;
    }

    public async Task UpdateAsync(UpdateHistory entry, CancellationToken cancellationToken = default)
    {
        _context.UpdateHistory.Update(entry);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<UpdateHistory?> GetActiveOperationAsync(CancellationToken cancellationToken = default)
    {
        return await _context.UpdateHistory
            .Where(h => ActiveStatuses.Contains(h.Status))
            .OrderBy(h => h.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<UpdateHistory?> GetLastSuccessfulUpdateAsync(CancellationToken cancellationToken = default)
    {
        return await _context.UpdateHistory
            .Where(h => h.OperationType == UpdateOperationType.Update && h.Status == UpdateOperationStatus.Succeeded)
            .OrderByDescending(h => h.CompletedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<UpdateHistory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.UpdateHistory
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<UpdateHistory>> GetRecentAsync(int take, CancellationToken cancellationToken = default)
    {
        return await _context.UpdateHistory
            .OrderByDescending(h => h.RequestedAt)
            .Take(take)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
