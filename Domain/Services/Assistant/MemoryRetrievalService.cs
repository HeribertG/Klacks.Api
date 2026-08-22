// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Retrieves pinned and relevant agent memories via hybrid search (embedding + keyword), then
/// silently expands into any remaining per-turn slots with 1-hop memory-relation neighbours of the
/// hybrid matches. Parallelizes embedding generation with DB queries for lower latency. Returns the
/// ids of every memory it injected alongside the rendered text, so a same-turn get_ai_memories call
/// can avoid re-surfacing the same content.
/// </summary>
/// <param name="memoryRepository">Repository for agent memory queries and access tracking</param>
/// <param name="embeddingService">Service for generating text embeddings</param>
/// <param name="expander">Best-effort 1-hop memory-relation expansion of the hybrid matches</param>
/// <param name="scopeFactory">
/// Resolves a separate service scope for the fire-and-forget access-count update. That write is
/// deliberately not awaited, so running it on the injected repository put a second operation on the
/// request-scoped DbContext while the turn was still querying it — EF threw "A second operation was
/// started on this context instance", the task's own catch swallowed it, and the access counts were
/// silently lost (observed live 2026-08-10). Its own scope owns its own DbContext and can also
/// outlive the request without touching a disposed one.
/// </param>
/// <param name="logger">Logger for warning on access count update and expansion failures</param>

using System.Text;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public class MemoryRetrievalService : IMemoryRetrievalService
{
    private readonly IAgentMemoryRepository _memoryRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly IMemoryRetrievalExpander _expander;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MemoryRetrievalService> _logger;

    // Reference-tier defaults, used whenever the caller does not (yet) know the per-turn budget
    // profile — mirrors ContextBudgetPolicy's reference anchor so behavior is unchanged for any
    // caller that has not been wired to pass a profile.
    private const int DefaultMaxMemoriesPerTurn = 5;
    private const int DefaultMaxPinnedMemories = 10;

    // Second line of defence behind the relevance filter in AgentMemoryRepository.ExecuteTextSearchAsync.
    // Memory content was injected verbatim with no cap of any kind, and the seeded knowledge documents
    // reach 24 404 characters (measured 2026-08-03), so a single relevant hit could still outweigh the
    // whole tool payload. The cap bounds what one memory may cost the prompt; the count caps above bound
    // how many there are. 1500 characters (~375 tokens) keeps a genuinely relevant memory useful while
    // making the worst case per turn predictable rather than open-ended.
    private const int MaxCharsPerMemory = 1500;
    private const string TruncationMarker = " […]";

    public MemoryRetrievalService(
        IAgentMemoryRepository memoryRepository,
        IEmbeddingService embeddingService,
        IMemoryRetrievalExpander expander,
        IServiceScopeFactory scopeFactory,
        ILogger<MemoryRetrievalService> logger)
    {
        _memoryRepository = memoryRepository;
        _embeddingService = embeddingService;
        _expander = expander;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Bumps the access counts of the memories injected this turn, on a scope of its own. The caller
    /// does not await this, so it must never touch the request-scoped DbContext: that context is still
    /// serving the turn and permits only one operation at a time.
    /// </summary>
    private async Task UpdateAccessCountsInOwnScopeAsync(List<Guid> memoryIds)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IAgentMemoryRepository>();
            await repository.UpdateAccessCountsAsync(memoryIds);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update memory access counts");
        }
    }

    public async Task<List<AgentMemory>> RetrieveToolsetLessonsAsync(
        Guid agentId,
        IReadOnlyList<string> skillNames,
        int maxLessons,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _memoryRepository.GetByCategoryAndKeysAsync(
                agentId, Klacks.Api.Domain.Constants.MemoryCategories.Reflection, skillNames, maxLessons, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Toolset lesson retrieval failed for agent {AgentId}; omitting the lessons block", agentId);
            return new List<AgentMemory>();
        }
    }

    public async Task<MemoryRetrievalResult> RetrieveRelevantMemoriesAsync(
        Guid agentId,
        string userMessage,
        Guid? userId = null,
        ContextBudgetProfile? budgetProfile = null,
        CancellationToken cancellationToken = default)
    {
        var maxPinnedMemories = budgetProfile?.MaxPinnedMemories ?? DefaultMaxPinnedMemories;
        var maxMemoriesPerTurn = budgetProfile?.MaxMemoriesPerTurn ?? DefaultMaxMemoriesPerTurn;

        var embeddingTask = _embeddingService.IsAvailable
            ? _embeddingService.GenerateEmbeddingAsync(userMessage, cancellationToken)
            : Task.FromResult<float[]?>(null);

        var pinnedMemories = await _memoryRepository.GetPinnedAsync(agentId, userId, cancellationToken);

        var queryEmbedding = await embeddingTask;

        var searchResults = await _memoryRepository.HybridSearchAsync(
            agentId, userMessage, queryEmbedding, maxMemoriesPerTurn, userId, cancellationToken);

        var allMemoryIds = searchResults.Select(r => r.Id).ToList();
        if (allMemoryIds.Count > 0)
        {
            _ = Task.Run(() => UpdateAccessCountsInOwnScopeAsync(allMemoryIds), CancellationToken.None);
        }

        var hasMemories = pinnedMemories.Count > 0 || searchResults.Count > 0;
        if (!hasMemories)
        {
            return new MemoryRetrievalResult(string.Empty, Array.Empty<Guid>());
        }

        var expansionMemories = await TryExpandAsync(agentId, pinnedMemories, searchResults, maxMemoriesPerTurn, userId, cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("=== PERSISTENT KNOWLEDGE ===");

        var pinnedTaken = pinnedMemories.Take(maxPinnedMemories).ToList();
        if (pinnedTaken.Count > 0)
        {
            sb.AppendLine("[PINNED]");
            foreach (var m in pinnedTaken)
            {
                sb.AppendLine($"- [{m.Category}] {m.Key}: {Cap(m.Content)}");
            }
        }

        if (searchResults.Count > 0)
        {
            sb.AppendLine("[RELEVANT]");
            foreach (var m in searchResults)
            {
                sb.AppendLine($"- [{m.Category}] {m.Key}: {Cap(m.Content)}");
            }
        }

        if (expansionMemories.Count > 0)
        {
            sb.AppendLine("[RELATED]");
            foreach (var m in expansionMemories)
            {
                sb.AppendLine($"- [{m.Category}] {m.Key}: {Cap(m.Content)}");
            }
        }

        sb.AppendLine("============================");

        var injectedIds = pinnedTaken.Select(m => m.Id)
            .Concat(searchResults.Select(r => r.Id))
            .Concat(expansionMemories.Select(m => m.Id))
            .ToList();

        return new MemoryRetrievalResult(sb.ToString(), injectedIds);
    }

    private static string Cap(string content)
    {
        if (string.IsNullOrEmpty(content) || content.Length <= MaxCharsPerMemory)
        {
            return content;
        }

        return string.Concat(content.AsSpan(0, MaxCharsPerMemory), TruncationMarker);
    }

    private async Task<IReadOnlyList<AgentMemory>> TryExpandAsync(
        Guid agentId,
        List<AgentMemory> pinnedMemories,
        List<MemorySearchResult> searchResults,
        int maxMemoriesPerTurn,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        var freeBudget = maxMemoriesPerTurn - searchResults.Count;
        if (freeBudget <= 0 || searchResults.Count == 0)
        {
            return Array.Empty<AgentMemory>();
        }

        try
        {
            return await _expander.ExpandAsync(agentId, pinnedMemories, searchResults, freeBudget, userId, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Memory retrieval expansion failed for agent {AgentId}", agentId);
            return Array.Empty<AgentMemory>();
        }
    }
}
