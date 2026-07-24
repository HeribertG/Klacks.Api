// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IProactiveTriggerDispatchRepository
{
    Task<bool> WasDispatchedAsync(string userId, string triggerKind, string dedupKey, CancellationToken cancellationToken = default);

    Task RecordAsync(ProactiveTriggerDispatchRow row, CancellationToken cancellationToken = default);

    Task<ProactiveTriggerDispatchRow?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task UpdateAsync(ProactiveTriggerDispatchRow row, CancellationToken cancellationToken = default);
}
