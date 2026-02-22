// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class AgentSkillRepository : IAgentSkillRepository
{
    private readonly DataBaseContext _context;

    public AgentSkillRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<List<AgentSkill>> GetEnabledAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        return await _context.AgentSkills
            .Where(s => s.AgentId == agentId && s.IsEnabled)
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<AgentSkill?> GetByNameAsync(Guid agentId, string name, CancellationToken cancellationToken = default)
    {
        return await _context.AgentSkills
            .FirstOrDefaultAsync(s => s.AgentId == agentId && s.Name == name, cancellationToken);
    }

    public async Task<AgentSkill?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AgentSkills
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task AddAsync(AgentSkill skill, CancellationToken cancellationToken = default)
    {
        await _context.AgentSkills.AddAsync(skill, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(AgentSkill skill, CancellationToken cancellationToken = default)
    {
        _context.AgentSkills.Update(skill);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var skill = await _context.AgentSkills
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (skill != null)
        {
            _context.AgentSkills.Remove(skill);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task LogExecutionAsync(AgentSkillExecution execution, CancellationToken cancellationToken = default)
    {
        _context.AgentSkillExecutions.Add(execution);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> GetSessionCallCountAsync(Guid skillId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _context.AgentSkillExecutions
            .Where(e => e.SkillId == skillId && e.SessionId == sessionId)
            .CountAsync(cancellationToken);
    }
}
