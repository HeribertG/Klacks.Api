using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class AgentMemoryRepository : IAgentMemoryRepository
{
    private readonly DataBaseContext _context;
    private readonly ILogger<AgentMemoryRepository> _logger;

    private const float WeightVector = 0.50f;
    private const float WeightFullText = 0.20f;
    private const float WeightImportance = 0.15f;
    private const float WeightRecency = 0.10f;
    private const float WeightAccess = 0.05f;

    private const float FallbackWeightFullText = 0.40f;
    private const float FallbackWeightImportance = 0.30f;
    private const float FallbackWeightRecency = 0.20f;
    private const float FallbackWeightAccess = 0.10f;

    private const int CandidatePool = 50;
    private const float DecayFactor = 0.01f;
    private const float MinVectorScore = 0.30f;

    public AgentMemoryRepository(DataBaseContext context, ILogger<AgentMemoryRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<MemorySearchResult>> HybridSearchAsync(
        Guid agentId, string query, float[]? queryEmbedding, int limit = 10, CancellationToken cancellationToken = default)
    {
        if (queryEmbedding is { Length: > 0 })
        {
            try
            {
                return await ExecuteVectorHybridSearchAsync(agentId, query, queryEmbedding, limit, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Vector hybrid search failed, falling back to text-only search");
            }
        }

        return await ExecuteTextSearchAsync(agentId, query, limit, cancellationToken);
    }

    private async Task<List<MemorySearchResult>> ExecuteVectorHybridSearchAsync(
        Guid agentId, string query, float[] queryEmbedding, int limit, CancellationToken cancellationToken)
    {
        var embeddingString = $"[{string.Join(",", queryEmbedding)}]";

        var results = await _context.Database
            .SqlQuery<MemorySearchRow>($"""
                WITH candidates AS (
                    SELECT
                        m.id,
                        m.content,
                        m.key,
                        m.category,
                        m.importance,
                        m.access_count,
                        m.is_pinned,
                        CASE WHEN m.embedding IS NOT NULL
                             THEN 1 - (m.embedding <=> {embeddingString}::vector)
                             ELSE 0
                        END AS vec_score,
                        ts_rank_cd(
                            setweight(to_tsvector('german', COALESCE(m.key, '')), 'A') ||
                            setweight(to_tsvector('german', m.content), 'B'),
                            plainto_tsquery('german', {query}),
                            32
                        ) AS fts_score,
                        EXTRACT(EPOCH FROM (NOW() - COALESCE(m.update_time, m.create_time))) / 86400.0 AS age_days
                    FROM agent_memories m
                    WHERE m.agent_id = {agentId}
                      AND m.is_deleted = false
                      AND m.is_pinned = false
                      AND (m.expires_at IS NULL OR m.expires_at > NOW())
                    ORDER BY m.embedding <=> {embeddingString}::vector
                    LIMIT {CandidatePool}
                ),
                scored AS (
                    SELECT *,
                        CAST((
                            {WeightVector}    * vec_score
                          + {WeightFullText}  * LEAST(COALESCE(fts_score, 0), 1.0)
                          + {WeightImportance} * (importance / 10.0)
                          + {WeightRecency}   * EXP(-{DecayFactor} * age_days)
                          + {WeightAccess}    * LEAST(access_count / 50.0, 1.0)
                        ) AS real) AS final_score
                    FROM candidates
                    WHERE vec_score >= {MinVectorScore} OR fts_score > 0 OR importance >= 7
                )
                SELECT id, content, key, category, importance, final_score AS score, is_pinned
                FROM scored
                ORDER BY final_score DESC
                LIMIT {limit}
                """)
            .ToListAsync(cancellationToken);

        return results.Select(r => new MemorySearchResult(r.Id, r.Content, r.Key, r.Category, r.Importance, r.Score, r.IsPinned)).ToList();
    }

    private async Task<List<MemorySearchResult>> ExecuteTextSearchAsync(
        Guid agentId, string query, int limit, CancellationToken cancellationToken)
    {
        var results = await _context.Database
            .SqlQuery<MemorySearchRow>($"""
                WITH candidates AS (
                    SELECT
                        m.id,
                        m.content,
                        m.key,
                        m.category,
                        m.importance,
                        m.access_count,
                        m.is_pinned,
                        ts_rank_cd(
                            setweight(to_tsvector('german', COALESCE(m.key, '')), 'A') ||
                            setweight(to_tsvector('german', m.content), 'B'),
                            plainto_tsquery('german', {query}),
                            32
                        ) AS fts_score,
                        EXTRACT(EPOCH FROM (NOW() - COALESCE(m.update_time, m.create_time))) / 86400.0 AS age_days
                    FROM agent_memories m
                    WHERE m.agent_id = {agentId}
                      AND m.is_deleted = false
                      AND m.is_pinned = false
                      AND (m.expires_at IS NULL OR m.expires_at > NOW())
                )
                SELECT id, content, key, category, importance,
                       CAST((
                           {FallbackWeightFullText} * LEAST(COALESCE(fts_score, 0), 1.0)
                         + {FallbackWeightImportance} * (importance / 10.0)
                         + {FallbackWeightRecency} * EXP(-{DecayFactor} * age_days)
                         + {FallbackWeightAccess} * LEAST(access_count / 50.0, 1.0)
                       ) AS real) AS score,
                       is_pinned
                FROM candidates
                WHERE fts_score > 0 OR importance >= 7
                ORDER BY score DESC
                LIMIT {limit}
                """)
            .ToListAsync(cancellationToken);

        return results.Select(r => new MemorySearchResult(r.Id, r.Content, r.Key, r.Category, r.Importance, r.Score, r.IsPinned)).ToList();
    }

    public async Task<List<AgentMemory>> GetPinnedAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        return await _context.AgentMemories
            .Where(m => m.AgentId == agentId && m.IsPinned && (m.ExpiresAt == null || m.ExpiresAt > DateTime.UtcNow))
            .OrderByDescending(m => m.Importance)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AgentMemory>> GetAllAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        return await _context.AgentMemories
            .Where(m => m.AgentId == agentId)
            .OrderByDescending(m => m.Importance)
            .ThenByDescending(m => m.CreateTime)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AgentMemory>> GetByCategoryAsync(Guid agentId, string category, CancellationToken cancellationToken = default)
    {
        return await _context.AgentMemories
            .Where(m => m.AgentId == agentId && m.Category == category)
            .OrderByDescending(m => m.Importance)
            .ThenByDescending(m => m.CreateTime)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<AgentMemory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AgentMemories
            .Include(m => m.Tags)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<List<AgentMemory>> SearchAsync(Guid agentId, string searchTerm, CancellationToken cancellationToken = default)
    {
        return await _context.AgentMemories
            .Where(m => m.AgentId == agentId &&
                        (EF.Functions.ILike(m.Key, $"%{searchTerm}%") ||
                         EF.Functions.ILike(m.Content, $"%{searchTerm}%")))
            .OrderByDescending(m => m.Importance)
            .ThenByDescending(m => m.CreateTime)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(AgentMemory memory, CancellationToken cancellationToken = default)
    {
        await _context.AgentMemories.AddAsync(memory, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(AgentMemory memory, CancellationToken cancellationToken = default)
    {
        _context.AgentMemories.Update(memory);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var memory = await _context.AgentMemories
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (memory != null)
        {
            _context.AgentMemories.Remove(memory);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<List<AgentMemory>> GetPendingEmbeddingsAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        return await _context.AgentMemories
            .Where(m => m.Embedding == null)
            .OrderBy(m => m.CreateTime)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        await _context.Database.ExecuteSqlRawAsync("""
            UPDATE agent_memories SET is_deleted = true, deleted_time = NOW()
            WHERE expires_at IS NOT NULL
              AND expires_at < NOW() - INTERVAL '1 hour'
              AND is_pinned = false
              AND is_deleted = false
            """, cancellationToken);
    }

    public async Task UpdateAccessCountsAsync(List<Guid> memoryIds, CancellationToken cancellationToken = default)
    {
        if (memoryIds.Count == 0) return;

        var memories = await _context.AgentMemories
            .Where(m => memoryIds.Contains(m.Id))
            .ToListAsync(cancellationToken);

        foreach (var memory in memories)
        {
            memory.AccessCount++;
            memory.LastAccessedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private class MemorySearchRow
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int Importance { get; set; }
        public float Score { get; set; }
        public bool IsPinned { get; set; }
    }
}
