// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IMemoryRetrievalExpander
{
    Task<IReadOnlyList<AgentMemory>> ExpandAsync(
        Guid agentId,
        IReadOnlyList<AgentMemory> pinnedMemories,
        IReadOnlyList<MemorySearchResult> hybridResults,
        int freeBudget,
        CancellationToken cancellationToken = default);
}
