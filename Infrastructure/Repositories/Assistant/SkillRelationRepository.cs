// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for the SkillRelation edges of the emergent skill-relationship graph, scoped per agent.
/// </summary>
/// <param name="context">Database context with the SkillRelations DbSet.</param>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class SkillRelationRepository : ISkillRelationRepository
{
    private readonly DataBaseContext _context;

    public SkillRelationRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<List<SkillRelation>> GetByAgentAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        return await _context.SkillRelations
            .Where(r => r.AgentId == agentId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<SkillRelation> relations, CancellationToken cancellationToken = default)
    {
        await _context.SkillRelations.AddRangeAsync(relations, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(SkillRelation relation, CancellationToken cancellationToken = default)
    {
        _context.SkillRelations.Update(relation);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
