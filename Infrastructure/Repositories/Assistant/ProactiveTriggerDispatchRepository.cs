// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF-backed dedup log for proactive triggers: remembers which (user, kind, content) alerts were
/// already sent so a recurring scan never re-sends the same alert (persisted across restarts).
/// Also loads and updates single dispatch rows so a user's reaction can be stored on them.
/// </summary>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class ProactiveTriggerDispatchRepository : IProactiveTriggerDispatchRepository
{
    private readonly DataBaseContext _context;

    public ProactiveTriggerDispatchRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<bool> WasDispatchedAsync(string userId, string triggerKind, string dedupKey, CancellationToken cancellationToken = default)
    {
        return await _context.AgentTriggerDispatches
            .AnyAsync(d => d.UserId == userId && d.TriggerKind == triggerKind && d.DedupKey == dedupKey, cancellationToken);
    }

    public async Task RecordAsync(ProactiveTriggerDispatchRow row, CancellationToken cancellationToken = default)
    {
        var alreadyRecorded = await WasDispatchedAsync(row.UserId, row.TriggerKind, row.DedupKey, cancellationToken);
        if (alreadyRecorded)
        {
            return;
        }

        await _context.AgentTriggerDispatches.AddAsync(row, cancellationToken);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            _context.Entry(row).State = EntityState.Detached;
            throw;
        }
    }

    public async Task<ProactiveTriggerDispatchRow?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AgentTriggerDispatches
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task UpdateAsync(ProactiveTriggerDispatchRow row, CancellationToken cancellationToken = default)
    {
        _context.AgentTriggerDispatches.Update(row);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
