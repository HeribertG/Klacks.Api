// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IMemoryRelationRepository
{
    Task<List<Guid>> NeighboursOfAsync(
        Guid agentId, IReadOnlyList<Guid> memoryIds, double minConfidence, int take, CancellationToken cancellationToken = default);

    Task<List<MemoryRelation>> GetByAgentAsync(Guid agentId, CancellationToken cancellationToken = default);

    Task AddOrUpdateAsync(MemoryRelation relation, CancellationToken cancellationToken = default);
}
