// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IProactiveTriggerDispatchRepository
{
    /// <summary>
    /// Whether a row for (user, kind, dedup key) already exists. A null <paramref name="conditionId"/>
    /// keeps the old behaviour and ignores the condition link entirely; a non-null value narrows the
    /// check to rows of that very condition, so a recurrence (new AgentCondition with a new id under the
    /// same fingerprint) is treated as not dispatched and gets its own row.
    /// </summary>
    Task<bool> WasDispatchedAsync(string userId, string triggerKind, string dedupKey, Guid? conditionId, CancellationToken cancellationToken = default);

    Task RecordAsync(ProactiveTriggerDispatchRow row, CancellationToken cancellationToken = default);

    Task<ProactiveTriggerDispatchRow?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task UpdateAsync(ProactiveTriggerDispatchRow row, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProactiveTriggerDispatchRow>> ListForUserAsync(string userId, bool unreadOnly, int take, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProactiveTriggerDispatchRow>> GetRecentReactionsAsync(string userId, string triggerKind, int take, CancellationToken cancellationToken = default);

    Task<int> CountUnreadAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// All dispatch rows (any user, any content state) recorded at or after <paramref name="sinceUtc"/>,
    /// newest first and capped at <paramref name="maxRows"/>. Used by the goal reflection pipeline to
    /// aggregate recurring signals across users; unlike ListForUserAsync this is not scoped to one user
    /// and does not exclude ledger-only rows (ContentKey == null), because a trigger firing without a
    /// persisted inbox message is still a real occurrence for aggregation purposes.
    /// </summary>
    Task<IReadOnlyList<ProactiveTriggerDispatchRow>> GetSinceAsync(DateTime sinceUtc, int maxRows, CancellationToken cancellationToken = default);

    Task<bool> MarkReadAsync(Guid id, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the given rows of that user as read. Ids the user does not own and rows already read
    /// are ignored, so a client can safely resend the whole page it currently shows.
    /// </summary>
    Task MarkManyReadAsync(IReadOnlyList<Guid> ids, string userId, CancellationToken cancellationToken = default);

    Task MarkAllReadAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dispatch rows whose next reminder is due at or before <paramref name="nowUtc"/>, oldest due date
    /// first, capped at <paramref name="take"/>. Only real inbox messages take part in the reminder
    /// loop: ledger-only rows (ContentKey == null) and rows not linked to a condition are excluded, as
    /// is every acknowledged row - acknowledgement is the only stop truth.
    /// </summary>
    Task<IReadOnlyList<ProactiveTriggerDispatchRow>> GetDueForReminderAsync(DateTime nowUtc, int take, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compare-and-swap for the reminder sweep: advances the row only when its NextReminderAtUtc still
    /// equals <paramref name="expectedDueUtc"/> and it is not acknowledged, so a concurrent acknowledge
    /// or a second sweep instance cannot double-send. Stamps ReminderCount + 1, LastRemindedAtUtc, the
    /// next due date and resets ReadAtUtc so the reminder surfaces as unread again.
    /// Returns true when exactly the expected row was advanced.
    /// </summary>
    Task<bool> TryAdvanceReminderAsync(Guid id, DateTime expectedDueUtc, DateTime remindedAtUtc, DateTime nextDueUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Same compare-and-swap guard as TryAdvanceReminderAsync, but only moves NextReminderAtUtc
    /// (null stops the row) without counting a reminder - for rescheduling without a send.
    /// </summary>
    Task<bool> TryRescheduleReminderAsync(Guid id, DateTime expectedDueUtc, DateTime? nextDueUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Acknowledges the row for its owner: stamps AcknowledgedAtUtc on first acknowledgement and clears
    /// NextReminderAtUtc, which ends the reminder loop. Rows of another user and unknown ids return
    /// false; acknowledging an already acknowledged row is idempotent and keeps the first timestamp.
    /// </summary>
    Task<bool> AcknowledgeAsync(Guid id, string userId, CancellationToken cancellationToken = default);
}
