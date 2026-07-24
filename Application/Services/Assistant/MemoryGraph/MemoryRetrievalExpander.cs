// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Silent retrieval expansion for agent memories: after the hybrid semantic search, pulls in
/// high-confidence active 1-hop neighbours of the matched memories to improve recall without an
/// extra get_ai_memories round-trip. It only fills FREE per-turn memory slots (never evicts a pinned
/// or hybrid-matched memory), is capped, and is best-effort — the caller decides how to handle
/// failures, this class only shapes the candidate selection.
/// </summary>
/// <param name="relationRepository">Source of the memory-relationship edges for the agent</param>
/// <param name="memoryRepository">Loads the full content of the selected neighbour memories</param>
/// <param name="logger">Diagnostic logging</param>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.MemoryGraph;

public class MemoryRetrievalExpander : IMemoryRetrievalExpander
{
    private readonly IMemoryRelationRepository _relationRepository;
    private readonly IAgentMemoryRepository _memoryRepository;
    private readonly ILogger<MemoryRetrievalExpander> _logger;

    public MemoryRetrievalExpander(
        IMemoryRelationRepository relationRepository, IAgentMemoryRepository memoryRepository, ILogger<MemoryRetrievalExpander> logger)
    {
        _relationRepository = relationRepository;
        _memoryRepository = memoryRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AgentMemory>> ExpandAsync(
        Guid agentId,
        IReadOnlyList<AgentMemory> pinnedMemories,
        IReadOnlyList<MemorySearchResult> hybridResults,
        int freeBudget,
        CancellationToken cancellationToken = default)
    {
        if (freeBudget <= 0 || hybridResults.Count == 0)
        {
            return Array.Empty<AgentMemory>();
        }

        var slots = Math.Min(freeBudget, MemoryGraphConstants.MaxExpansionSlots);
        var seedIds = hybridResults.Select(r => r.Id).ToList();
        var excludeIds = new HashSet<Guid>(seedIds);
        foreach (var pinned in pinnedMemories)
        {
            excludeIds.Add(pinned.Id);
        }

        var candidateIds = await _relationRepository.NeighboursOfAsync(
            agentId, seedIds, MemoryGraphConstants.NeighbourMinConfidence, slots, cancellationToken);

        var pickedIds = BuildExpansionIds(candidateIds, excludeIds, slots);
        if (pickedIds.Count == 0)
        {
            return Array.Empty<AgentMemory>();
        }

        var memories = await _memoryRepository.GetByIdsAsync(pickedIds, cancellationToken);
        var memoriesById = memories.ToDictionary(m => m.Id);
        var ordered = pickedIds.Where(memoriesById.ContainsKey).Select(id => memoriesById[id]).ToList();

        _logger.LogDebug("Memory retrieval expansion added {Count} neighbour memories for agent {AgentId}", ordered.Count, agentId);
        return ordered;
    }

    public static IReadOnlyList<Guid> BuildExpansionIds(IReadOnlyList<Guid> candidateIds, IReadOnlySet<Guid> excludeIds, int slots)
    {
        if (slots <= 0)
        {
            return Array.Empty<Guid>();
        }

        var seen = new HashSet<Guid>();
        var result = new List<Guid>();
        foreach (var candidateId in candidateIds)
        {
            if (excludeIds.Contains(candidateId) || !seen.Add(candidateId))
            {
                continue;
            }

            result.Add(candidateId);
            if (result.Count >= slots)
            {
                break;
            }
        }

        return result;
    }
}
