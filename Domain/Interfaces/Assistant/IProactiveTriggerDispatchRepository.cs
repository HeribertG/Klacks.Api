// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IProactiveTriggerDispatchRepository
{
    Task<bool> WasDispatchedAsync(string userId, string triggerKind, string dedupKey, CancellationToken cancellationToken = default);

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

    Task MarkAllReadAsync(string userId, CancellationToken cancellationToken = default);
}
