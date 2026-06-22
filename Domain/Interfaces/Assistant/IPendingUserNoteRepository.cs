// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IPendingUserNoteRepository
{
    Task<List<PendingUserNote>> GetPendingAsync(Guid agentId, Guid userId, CancellationToken cancellationToken = default);
    Task<List<PendingUserNote>> GetDeliveredAsync(Guid agentId, Guid userId, CancellationToken cancellationToken = default);
    Task<int> CountPendingAsync(Guid agentId, Guid userId, CancellationToken cancellationToken = default);
    Task<PendingUserNote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(PendingUserNote note, CancellationToken cancellationToken = default);
    Task<int> MarkDeliveredAsync(Guid agentId, Guid userId, IReadOnlyCollection<Guid> noteIds, CancellationToken cancellationToken = default);
    Task<int> ExpireBroadcastsAsync(TimeSpan maxAge, CancellationToken cancellationToken = default);
}
