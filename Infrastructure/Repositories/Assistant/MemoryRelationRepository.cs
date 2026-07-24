// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for the MemoryRelation edges of the emergent memory-relationship graph, scoped per
/// agent. NeighboursOfAsync joins the candidate ids back against the AgentMemories DbSet so its
/// soft-delete query filter excludes neighbours whose memory has since been deleted — an active edge
/// is not itself removed when one endpoint is soft-deleted (cascade delete only fires on hard delete).
/// </summary>
/// <param name="context">Database context with the MemoryRelations and AgentMemories DbSets</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.ValueObjects;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class MemoryRelationRepository : IMemoryRelationRepository
{
    private readonly DataBaseContext _context;

    public MemoryRelationRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<List<Guid>> NeighboursOfAsync(
        Guid agentId, IReadOnlyList<Guid> memoryIds, double minConfidence, int take, CancellationToken cancellationToken = default)
    {
        if (memoryIds.Count == 0 || take <= 0)
        {
            return new List<Guid>();
        }

        var edges = await _context.MemoryRelations
            .Where(r => r.AgentId == agentId
                && r.Status == MemoryRelationStatus.Active
                && r.Confidence >= minConfidence
                && (memoryIds.Contains(r.MemoryAId) || memoryIds.Contains(r.MemoryBId)))
            .Select(r => new { r.MemoryAId, r.MemoryBId, r.Confidence })
            .ToListAsync(cancellationToken);

        var bestConfidenceByCandidate = new Dictionary<Guid, double>();
        foreach (var edge in edges)
        {
            var candidate = memoryIds.Contains(edge.MemoryAId) ? edge.MemoryBId : edge.MemoryAId;
            if (memoryIds.Contains(candidate))
            {
                continue;
            }

            if (!bestConfidenceByCandidate.TryGetValue(candidate, out var best) || edge.Confidence > best)
            {
                bestConfidenceByCandidate[candidate] = edge.Confidence;
            }
        }

        if (bestConfidenceByCandidate.Count == 0)
        {
            return new List<Guid>();
        }

        var orderedCandidateIds = bestConfidenceByCandidate
            .OrderByDescending(kv => kv.Value)
            .Select(kv => kv.Key)
            .ToList();

        var activeCandidateIds = await _context.AgentMemories
            .Where(m => orderedCandidateIds.Contains(m.Id))
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);
        var activeCandidateSet = new HashSet<Guid>(activeCandidateIds);

        return orderedCandidateIds
            .Where(id => activeCandidateSet.Contains(id))
            .Take(take)
            .ToList();
    }

    public async Task<List<MemoryRelation>> GetByAgentAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        return await _context.MemoryRelations
            .Where(r => r.AgentId == agentId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddOrUpdateAsync(MemoryRelation relation, CancellationToken cancellationToken = default)
    {
        var canonical = MemoryRelationPair.Canonical(relation.MemoryAId, relation.MemoryBId);

        var existing = await _context.MemoryRelations.FirstOrDefaultAsync(
            r => r.AgentId == relation.AgentId
                && r.MemoryAId == canonical.MemoryAId
                && r.MemoryBId == canonical.MemoryBId
                && r.Type == relation.Type,
            cancellationToken);

        if (existing != null)
        {
            existing.Confidence = relation.Confidence;
            existing.Provenance = relation.Provenance;
            existing.Status = relation.Status;
        }
        else
        {
            await _context.MemoryRelations.AddAsync(
                new MemoryRelation
                {
                    Id = relation.Id == Guid.Empty ? Guid.NewGuid() : relation.Id,
                    AgentId = relation.AgentId,
                    MemoryAId = canonical.MemoryAId,
                    MemoryBId = canonical.MemoryBId,
                    Type = relation.Type,
                    Confidence = relation.Confidence,
                    Provenance = relation.Provenance,
                    Status = relation.Status,
                },
                cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
