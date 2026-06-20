// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IProactiveTriggerDispatchRepository
{
    Task<bool> WasDispatchedAsync(string userId, string triggerKind, string dedupKey, CancellationToken cancellationToken = default);

    Task RecordAsync(string userId, string triggerKind, string dedupKey, CancellationToken cancellationToken = default);
}
