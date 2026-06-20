// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF-backed dedup log for proactive triggers: remembers which (user, kind, content) alerts were
/// already sent so a recurring scan never re-sends the same alert (persisted across restarts).
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

    public async Task RecordAsync(string userId, string triggerKind, string dedupKey, CancellationToken cancellationToken = default)
    {
        var alreadyRecorded = await WasDispatchedAsync(userId, triggerKind, dedupKey, cancellationToken);
        if (alreadyRecorded)
        {
            return;
        }

        await _context.AgentTriggerDispatches.AddAsync(new ProactiveTriggerDispatchRow
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TriggerKind = triggerKind,
            DedupKey = dedupKey
        }, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
