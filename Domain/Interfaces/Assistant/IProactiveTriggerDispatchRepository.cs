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

    Task<bool> MarkReadAsync(Guid id, string userId, CancellationToken cancellationToken = default);

    Task MarkAllReadAsync(string userId, CancellationToken cancellationToken = default);
}
