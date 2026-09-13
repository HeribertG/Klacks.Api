// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAgentMemoryRepository
{
    Task<List<MemorySearchResult>> HybridSearchAsync(Guid agentId, string query, float[]? queryEmbedding, int limit = 10, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<List<AgentMemory>> GetPinnedAsync(Guid agentId, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<List<AgentMemory>> GetAllAsync(Guid agentId, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<List<AgentMemory>> GetAllWithTagsAsync(Guid agentId, CancellationToken cancellationToken = default);
    Task<List<AgentMemory>> GetByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default);
    Task<List<AgentMemory>> GetByCategoryAsync(Guid agentId, string category, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<List<AgentMemory>> GetByCategoryAndKeysAsync(Guid agentId, string category, IReadOnlyCollection<string> keys, int limit, CancellationToken cancellationToken = default);
    Task<List<AgentMemory>> GetByUserAsync(Guid agentId, Guid userId, CancellationToken cancellationToken = default);
    Task<AgentMemory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AgentMemory?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<List<AgentMemory>> SearchAsync(Guid agentId, string searchTerm, Guid? userId = null, CancellationToken cancellationToken = default);
    Task AddAsync(AgentMemory memory, CancellationToken cancellationToken = default);

    /// <summary>
    /// The live memory of this agent and scope that already carries the same normalized key or the same
    /// normalized content, or null when there is none. Normalization is MessageNormalizer.Normalize (NFC,
    /// trimmed, lower-cased, whitespace collapsed) on both sides, so casing and spacing never open a second
    /// copy of one fact. The scope is exact: a personal memory is only compared against the same user's
    /// memories, a shared one only against shared ones. The SQL candidate set excludes system_import rows
    /// and any memory longer than AgentMemoryDedupeLimits.MaxCandidateContentLength before it is loaded for
    /// normalization, since a short extracted fact can never equal either.
    /// An expired row is deliberately still returned even though no read path shows it: the caller needs it
    /// to push its expiry forward, because a bool answer would leave the fact blocked by a row nobody can
    /// see and nothing purges.
    /// </summary>
    Task<AgentMemory?> FindDuplicateAsync(
        Guid agentId,
        Guid? userId,
        string normalizedKey,
        string normalizedContent,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(AgentMemory memory, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<AgentMemory>> GetPendingEmbeddingsAsync(int limit = 100, CancellationToken cancellationToken = default);
    Task CleanupExpiredAsync(CancellationToken cancellationToken = default);
    Task UpdateAccessCountsAsync(List<Guid> memoryIds, CancellationToken cancellationToken = default);
    Task<int> AdjustImportanceByUsageAsync(CancellationToken cancellationToken = default);
    Task<int> CleanupLowValueMemoriesAsync(CancellationToken cancellationToken = default);
}
