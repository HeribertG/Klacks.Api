// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Retrieves pinned and relevant agent memories via hybrid search (embedding + keyword).
/// Parallelizes embedding generation with DB queries for lower latency.
/// </summary>
/// <param name="memoryRepository">Repository for agent memory queries and access tracking</param>
/// <param name="embeddingService">Service for generating text embeddings</param>
/// <param name="logger">Logger for warning on access count update failures</param>

using System.Text;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public class MemoryRetrievalService : IMemoryRetrievalService
{
    private readonly IAgentMemoryRepository _memoryRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<MemoryRetrievalService> _logger;

    // Reference-tier defaults, used whenever the caller does not (yet) know the per-turn budget
    // profile — mirrors ContextBudgetPolicy's reference anchor so behavior is unchanged for any
    // caller that has not been wired to pass a profile.
    private const int DefaultMaxMemoriesPerTurn = 5;
    private const int DefaultMaxPinnedMemories = 10;

    public MemoryRetrievalService(
        IAgentMemoryRepository memoryRepository,
        IEmbeddingService embeddingService,
        ILogger<MemoryRetrievalService> logger)
    {
        _memoryRepository = memoryRepository;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task<string> RetrieveRelevantMemoriesAsync(
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
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("=== PERSISTENT KNOWLEDGE ===");

        if (pinnedMemories.Count > 0)
        {
            sb.AppendLine("[PINNED]");
            foreach (var m in pinnedMemories.Take(maxPinnedMemories))
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

        sb.AppendLine("============================");

        return sb.ToString();
    }
}
