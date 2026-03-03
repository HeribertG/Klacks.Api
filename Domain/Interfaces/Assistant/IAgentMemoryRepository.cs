// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public record MemorySearchResult(Guid Id, string Content, string Key, string Category, int Importance, float Score, bool IsPinned);

public interface IAgentMemoryRepository
{
    Task<List<MemorySearchResult>> HybridSearchAsync(Guid agentId, string query, float[]? queryEmbedding, int limit = 10, CancellationToken cancellationToken = default);
    Task<List<AgentMemory>> GetPinnedAsync(Guid agentId, CancellationToken cancellationToken = default);
    Task<List<AgentMemory>> GetAllAsync(Guid agentId, CancellationToken cancellationToken = default);
    Task<List<AgentMemory>> GetByCategoryAsync(Guid agentId, string category, CancellationToken cancellationToken = default);
    Task<AgentMemory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<AgentMemory>> SearchAsync(Guid agentId, string searchTerm, CancellationToken cancellationToken = default);
    Task AddAsync(AgentMemory memory, CancellationToken cancellationToken = default);
    Task UpdateAsync(AgentMemory memory, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<AgentMemory>> GetPendingEmbeddingsAsync(int limit = 100, CancellationToken cancellationToken = default);
    Task CleanupExpiredAsync(CancellationToken cancellationToken = default);
    Task UpdateAccessCountsAsync(List<Guid> memoryIds, CancellationToken cancellationToken = default);
}
