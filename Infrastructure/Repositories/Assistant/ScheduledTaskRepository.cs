// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads and writes recurring (cron) tasks from the scheduled_task table. The due scan returns enabled,
/// non-deleted tasks whose next run has passed; <see cref="TryClaimAsync"/> advances the next run with a
/// conditional update so a tick or a second API instance cannot double-fire the same occurrence.
/// </summary>
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class ScheduledTaskRepository : IScheduledTaskRepository
{
    private readonly DataBaseContext _context;

    public ScheduledTaskRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<List<ScheduledTask>> GetDueAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        return await _context.ScheduledTasks
            .Where(t => t.IsEnabled && t.NextRunUtc != null && t.NextRunUtc <= nowUtc)
            .OrderBy(t => t.NextRunUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ScheduledTask>> GetByOwnerAsync(Guid ownerUserId, bool includeDisabled, CancellationToken cancellationToken = default)
    {
        var query = _context.ScheduledTasks.Where(t => t.OwnerUserId == ownerUserId);
        if (!includeDisabled)
        {
            query = query.Where(t => t.IsEnabled);
        }

        return await query
            .OrderBy(t => t.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<ScheduledTask?> GetByOwnerAndNameAsync(Guid ownerUserId, string name, CancellationToken cancellationToken = default)
    {
        return await _context.ScheduledTasks
            .FirstOrDefaultAsync(t => t.OwnerUserId == ownerUserId && t.Name == name, cancellationToken);
    }

    public async Task<ScheduledTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ScheduledTasks
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<bool> TryClaimAsync(Guid id, DateTime? expectedNextRunUtc, DateTime? newNextRunUtc, CancellationToken cancellationToken = default)
    {
        var affected = await _context.ScheduledTasks
            .Where(t => t.Id == id && t.NextRunUtc == expectedNextRunUtc)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(t => t.NextRunUtc, newNextRunUtc),
                cancellationToken);

        return affected > 0;
    }

    public async Task AddAsync(ScheduledTask task, CancellationToken cancellationToken = default)
    {
        await _context.ScheduledTasks.AddAsync(task, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ScheduledTask task, CancellationToken cancellationToken = default)
    {
        _context.ScheduledTasks.Update(task);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await _context.ScheduledTasks
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (task != null)
        {
            _context.ScheduledTasks.Remove(task);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
