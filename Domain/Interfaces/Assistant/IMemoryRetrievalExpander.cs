// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IMemoryRetrievalExpander
{
    Task<IReadOnlyList<AgentMemory>> ExpandAsync(
        Guid agentId,
        IReadOnlyList<AgentMemory> pinnedMemories,
        IReadOnlyList<MemorySearchResult> hybridResults,
        int freeBudget,
        Guid? userId = null,
        CancellationToken cancellationToken = default);
}
