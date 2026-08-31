// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF-backed dedup log for proactive triggers: remembers which (user, kind, content) alerts were
/// already sent so a recurring scan never re-sends the same alert (persisted across restarts).
/// Also serves as the proactive message inbox: lists a user's dispatch rows newest first,
/// counts unread rows and marks single rows or all rows of a user as read. Rows without a
/// ContentKey are dedup-ledger entries only (recorded before the inbox existed, or for
/// broadcasts that carry no persisted content) and are excluded from both the listing and the
/// unread count so they never render as an empty message.
/// For ledger-tracked rows the dedup check is narrowed by ConditionId, so a recurrence of the same
/// finding (new AgentCondition with a new id under the same fingerprint) deliberately gets its own
/// row instead of being swallowed by the old entry. Linked, unacknowledged rows additionally carry a
/// reminder schedule (NextReminderAtUtc): GetDueForReminderAsync feeds the sweep, the two Try*Reminder
/// methods advance or stop it under a compare-and-swap guard, and AcknowledgeAsync is the only stop.
/// </summary>
/// <param name="context">The database context.</param>
/// <param name="timeProvider">Clock ReadAtUtc is stamped from, injected so a test can drive it.</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class ProactiveTriggerDispatchRepository : IProactiveTriggerDispatchRepository
{
    private readonly DataBaseContext _context;
    private readonly TimeProvider _timeProvider;

    public ProactiveTriggerDispatchRepository(DataBaseContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public async Task<bool> WasDispatchedAsync(string userId, string triggerKind, string dedupKey, Guid? conditionId, CancellationToken cancellationToken = default)
    {
        var query = _context.AgentTriggerDispatches
            .Where(d => d.UserId == userId && d.TriggerKind == triggerKind && d.DedupKey == dedupKey);
        if (conditionId is not null)
        {
            query = query.Where(d => d.ConditionId == conditionId);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task RecordAsync(ProactiveTriggerDispatchRow row, CancellationToken cancellationToken = default)
    {
        var alreadyRecorded = await WasDispatchedAsync(row.UserId, row.TriggerKind, row.DedupKey, row.ConditionId, cancellationToken);
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
            row.ReadAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    public async Task MarkManyReadAsync(IReadOnlyList<Guid> ids, string userId, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return;
        }

        var rows = await _context.AgentTriggerDispatches
            .Where(d => d.UserId == userId && d.ReadAtUtc == null && ids.Contains(d.Id))
            .ToListAsync(cancellationToken);
        if (rows.Count == 0)
        {
            return;
        }

        var readAt = _timeProvider.GetUtcNow().UtcDateTime;
        foreach (var row in rows)
        {
            row.ReadAtUtc = readAt;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllReadAsync(string userId, CancellationToken cancellationToken = default)
    {
        var readAt = _timeProvider.GetUtcNow().UtcDateTime;
        await _context.AgentTriggerDispatches
            .Where(d => d.UserId == userId && d.ReadAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.ReadAtUtc, readAt), cancellationToken);
    }

    public async Task<IReadOnlyList<ProactiveTriggerDispatchRow>> GetDueForReminderAsync(DateTime nowUtc, int take, CancellationToken cancellationToken = default)
    {
        return await _context.AgentTriggerDispatches
            .AsNoTracking()
            .Where(d => d.ContentKey != null
                && d.ConditionId != null
                && d.AcknowledgedAtUtc == null
                && d.NextReminderAtUtc != null
                && d.NextReminderAtUtc <= nowUtc)
            .OrderBy(d => d.NextReminderAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> TryAdvanceReminderAsync(Guid id, DateTime expectedDueUtc, DateTime remindedAtUtc, DateTime nextDueUtc, CancellationToken cancellationToken = default)
    {
        var affected = await _context.AgentTriggerDispatches
            .Where(d => d.Id == id && d.NextReminderAtUtc == expectedDueUtc && d.AcknowledgedAtUtc == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.ReminderCount, d => d.ReminderCount + 1)
                .SetProperty(d => d.LastRemindedAtUtc, remindedAtUtc)
                .SetProperty(d => d.NextReminderAtUtc, nextDueUtc)
                .SetProperty(d => d.ReadAtUtc, (DateTime?)null), cancellationToken);
        return affected == 1;
    }

    public async Task<bool> TryRescheduleReminderAsync(Guid id, DateTime expectedDueUtc, DateTime? nextDueUtc, CancellationToken cancellationToken = default)
    {
        var affected = await _context.AgentTriggerDispatches
            .Where(d => d.Id == id && d.NextReminderAtUtc == expectedDueUtc && d.AcknowledgedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.NextReminderAtUtc, nextDueUtc), cancellationToken);
        return affected == 1;
    }

    public async Task<bool> AcknowledgeAsync(Guid id, string userId, CancellationToken cancellationToken = default)
    {
        var row = await _context.AgentTriggerDispatches
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (row == null || !string.Equals(row.UserId, userId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        row.AcknowledgedAtUtc ??= _timeProvider.GetUtcNow().UtcDateTime;
        row.NextReminderAtUtc = null;
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
