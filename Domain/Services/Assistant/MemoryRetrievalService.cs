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
    private readonly ILogger<MemoryRetrievalService> _logger;

    // Reference-tier defaults, used whenever the caller does not (yet) know the per-turn budget
    // profile — mirrors ContextBudgetPolicy's reference anchor so behavior is unchanged for any
    // caller that has not been wired to pass a profile.
    private const int DefaultMaxMemoriesPerTurn = 5;
    private const int DefaultMaxPinnedMemories = 10;

    public MemoryRetrievalService(
        IAgentMemoryRepository memoryRepository,
        IEmbeddingService embeddingService,
        IMemoryRetrievalExpander expander,
        ILogger<MemoryRetrievalService> logger)
    {
        _memoryRepository = memoryRepository;
        _embeddingService = embeddingService;
        _expander = expander;
        _logger = logger;
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
            _ = Task.Run(async () =>
            {
                try { await _memoryRepository.UpdateAccessCountsAsync(allMemoryIds); }
                catch (Exception ex) { _logger.LogWarning(ex, "Failed to update memory access counts"); }
            }, CancellationToken.None);
        }

        var hasMemories = pinnedMemories.Count > 0 || searchResults.Count > 0;
        if (!hasMemories)
        {
            return new MemoryRetrievalResult(string.Empty, Array.Empty<Guid>());
        }

        var expansionMemories = await TryExpandAsync(agentId, pinnedMemories, searchResults, maxMemoriesPerTurn, cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("=== PERSISTENT KNOWLEDGE ===");

        var pinnedTaken = pinnedMemories.Take(maxPinnedMemories).ToList();
        if (pinnedTaken.Count > 0)
        {
            sb.AppendLine("[PINNED]");
            foreach (var m in pinnedTaken)
            {
                sb.AppendLine($"- [{m.Category}] {m.Key}: {m.Content}");
            }
        }

        if (searchResults.Count > 0)
        {
            sb.AppendLine("[RELEVANT]");
            foreach (var m in searchResults)
            {
                sb.AppendLine($"- [{m.Category}] {m.Key}: {m.Content}");
            }
        }

        if (expansionMemories.Count > 0)
        {
            sb.AppendLine("[RELATED]");
            foreach (var m in expansionMemories)
            {
                sb.AppendLine($"- [{m.Category}] {m.Key}: {m.Content}");
            }
        }

        sb.AppendLine("============================");

        var injectedIds = pinnedTaken.Select(m => m.Id)
            .Concat(searchResults.Select(r => r.Id))
            .Concat(expansionMemories.Select(m => m.Id))
            .ToList();

        return new MemoryRetrievalResult(sb.ToString(), injectedIds);
    }

    private async Task<IReadOnlyList<AgentMemory>> TryExpandAsync(
        Guid agentId,
        List<AgentMemory> pinnedMemories,
        List<MemorySearchResult> searchResults,
        int maxMemoriesPerTurn,
        CancellationToken cancellationToken)
    {
        var freeBudget = maxMemoriesPerTurn - searchResults.Count;
        if (freeBudget <= 0 || searchResults.Count == 0)
        {
            return Array.Empty<AgentMemory>();
        }

        try
        {
            return await _expander.ExpandAsync(agentId, pinnedMemories, searchResults, freeBudget, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Memory retrieval expansion failed for agent {AgentId}", agentId);
            return Array.Empty<AgentMemory>();
        }
    }
}
