// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF-backed dedup log for proactive triggers: remembers which (user, kind, content) alerts were
/// already sent so a recurring scan never re-sends the same alert (persisted across restarts).
/// Also serves as the proactive message inbox: lists a user's dispatch rows newest first,
/// counts unread rows and marks single rows or all rows of a user as read. Rows without a
/// ContentKey are dedup-ledger entries only (recorded before the inbox existed, or for
/// broadcasts that carry no persisted content) and are excluded from both the listing and the
/// unread count so they never render as an empty message.
/// </summary>

using Klacks.Api.Domain.Enums;
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

    public async Task<IReadOnlyList<ProactiveTriggerDispatchRow>> ListForUserAsync(string userId, bool unreadOnly, int take, CancellationToken cancellationToken = default)
    {
        var query = InboxMessagesForUser(userId).AsNoTracking();

        if (unreadOnly)
        {
            query = query.Where(d => d.ReadAtUtc == null);
        }

        return await query
            .OrderByDescending(d => d.CreateTime)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProactiveTriggerDispatchRow>> GetRecentReactionsAsync(string userId, string triggerKind, int take, CancellationToken cancellationToken = default)
    {
        return await _context.AgentTriggerDispatches
            .AsNoTracking()
            .Where(d => d.UserId == userId && d.TriggerKind == triggerKind && d.Reaction != ProactiveReaction.None)
            .OrderByDescending(d => d.ReactionAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountUnreadAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await InboxMessagesForUser(userId)
            .CountAsync(d => d.ReadAtUtc == null, cancellationToken);
    }

    public async Task<IReadOnlyList<ProactiveTriggerDispatchRow>> GetSinceAsync(DateTime sinceUtc, int maxRows, CancellationToken cancellationToken = default)
    {
        return await _context.AgentTriggerDispatches
            .AsNoTracking()
            .Where(d => d.CreateTime >= sinceUtc)
            .OrderByDescending(d => d.CreateTime)
            .Take(maxRows)
            .ToListAsync(cancellationToken);
    }

    private IQueryable<ProactiveTriggerDispatchRow> InboxMessagesForUser(string userId)
    {
        return _context.AgentTriggerDispatches
            .Where(d => d.UserId == userId && d.ContentKey != null);
    }

    public async Task<bool> MarkReadAsync(Guid id, string userId, CancellationToken cancellationToken = default)
    {
        var row = await _context.AgentTriggerDispatches
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (row == null || !string.Equals(row.UserId, userId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (row.ReadAtUtc == null)
        {
            row.ReadAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    public async Task MarkAllReadAsync(string userId, CancellationToken cancellationToken = default)
    {
        var readAt = DateTime.UtcNow;
        await _context.AgentTriggerDispatches
            .Where(d => d.UserId == userId && d.ReadAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.ReadAtUtc, readAt), cancellationToken);
    }
}
