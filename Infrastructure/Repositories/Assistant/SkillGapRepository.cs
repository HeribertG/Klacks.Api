// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core repository for SkillGapRecord, including vector-based similarity search via pgvector.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class SkillGapRepository : ISkillGapRepository
{
    private readonly DataBaseContext _context;

    public SkillGapRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<SkillGapRecord?> FindSimilarAsync(
        Guid agentId,
        float[] embedding,
        float threshold,
        CancellationToken cancellationToken = default)
    {
        var embeddingString = $"[{string.Join(",", embedding)}]";

        var results = await _context.Database
            .SqlQuery<SkillGapSimilarityRow>($"""
                SELECT
                    id,
                    1 - (embedding <=> {embeddingString}::vector) AS similarity
                FROM skill_gap_records
                WHERE agent_id = {agentId}
                  AND status = {SkillGapStatuses.Detected}
                  AND embedding IS NOT NULL
                ORDER BY embedding <=> {embeddingString}::vector
                LIMIT 1
                """)
            .ToListAsync(cancellationToken);

        var top = results.FirstOrDefault();
        if (top == null || top.Similarity < threshold)
        {
            return null;
        }

        return await _context.SkillGapRecords
            .FirstOrDefaultAsync(r => r.Id == top.Id, cancellationToken);
    }

    public async Task AddAsync(SkillGapRecord record, CancellationToken cancellationToken = default)
    {
        await _context.SkillGapRecords.AddAsync(record, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(SkillGapRecord record, CancellationToken cancellationToken = default)
    {
        _context.SkillGapRecords.Update(record);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<SkillGapRecord>> GetPendingAsync(
        Guid agentId,
        int minOccurrences,
        CancellationToken cancellationToken = default)
    {
        return await _context.SkillGapRecords
            .Where(r => r.AgentId == agentId
                     && r.Status == SkillGapStatuses.Detected
                     && r.OccurrenceCount >= minOccurrences)
            .OrderByDescending(r => r.OccurrenceCount)
            .ToListAsync(cancellationToken);
    }

    private sealed class SkillGapSimilarityRow
    {
        public Guid Id { get; set; }
        public double Similarity { get; set; }
    }
}
