// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Deterministic, LLM-free population of the emergent memory-relationship graph: once per background
/// cleanup cycle it picks up to MaxMemoriesPerBuildRun memories that have no relation edge yet and
/// scores them against every other memory of the default agent via shared tags and embedding cosine
/// similarity (MemoryRelationCandidateBuilder). Winning candidates are stored as Active edges — there
/// is no reinforcement loop here, both signals are strong enough to create directly.
/// </summary>
/// <param name="agentRepository">Resolves the default agent scope</param>
/// <param name="memoryRepository">Source of agent memories (with tags and embeddings) to compare</param>
/// <param name="relationRepository">Persistence of the memory-relationship edges</param>
/// <param name="logger">Diagnostic logging of the build outcome</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.MemoryGraph;

public class MemoryRelationBuilder : IMemoryRelationBuilder
{
    private readonly IAgentRepository _agentRepository;
    private readonly IAgentMemoryRepository _memoryRepository;
    private readonly IMemoryRelationRepository _relationRepository;
    private readonly ILogger<MemoryRelationBuilder> _logger;

    public MemoryRelationBuilder(
        IAgentRepository agentRepository,
        IAgentMemoryRepository memoryRepository,
        IMemoryRelationRepository relationRepository,
        ILogger<MemoryRelationBuilder> logger)
    {
        _agentRepository = agentRepository;
        _memoryRepository = memoryRepository;
        _relationRepository = relationRepository;
        _logger = logger;
    }

    public async Task<int> BuildAsync(CancellationToken cancellationToken = default)
    {
        var agent = await _agentRepository.GetDefaultAgentAsync(cancellationToken);
        if (agent == null)
        {
            return 0;
        }

        var memories = await _memoryRepository.GetAllWithTagsAsync(agent.Id, cancellationToken);
        if (memories.Count < 2)
        {
            return 0;
        }

        var existingEdges = await _relationRepository.GetByAgentAsync(agent.Id, cancellationToken);
        var memoryIdsWithEdges = new HashSet<Guid>();
        foreach (var edge in existingEdges)
        {
            memoryIdsWithEdges.Add(edge.MemoryAId);
            memoryIdsWithEdges.Add(edge.MemoryBId);
        }

        var targets = memories
            .Where(m => !memoryIdsWithEdges.Contains(m.Id))
            .OrderByDescending(m => m.CreateTime)
            .Take(MemoryGraphConstants.MaxMemoriesPerBuildRun)
            .ToList();

        var builtCount = 0;
        foreach (var target in targets)
        {
            var candidates = MemoryRelationCandidateBuilder.ComputeCandidates(target, memories);
            foreach (var candidate in candidates)
            {
                await _relationRepository.AddOrUpdateAsync(
                    new MemoryRelation
                    {
                        Id = Guid.NewGuid(),
                        AgentId = agent.Id,
                        MemoryAId = target.Id,
                        MemoryBId = candidate.MemoryId,
                        Type = MemoryRelationType.Associative,
                        Confidence = candidate.Confidence,
                        Provenance = candidate.Provenance,
                        Status = MemoryRelationStatus.Active,
                    },
                    cancellationToken);
                builtCount++;
            }
        }

        if (builtCount > 0)
        {
            _logger.LogInformation(
                "Memory-relation build complete: {Count} edge(s) computed for {Targets} memory(ies) without edges.",
                builtCount, targets.Count);
        }

        return builtCount;
    }
}
