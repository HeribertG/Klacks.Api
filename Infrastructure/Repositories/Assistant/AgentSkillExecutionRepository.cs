// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF-backed read-only repository for AgentSkillExecution rows.
/// </summary>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class AgentSkillExecutionRepository : IAgentSkillExecutionRepository
{
    private readonly DataBaseContext _context;

    public AgentSkillExecutionRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<List<AgentSkillExecution>> GetSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken = default)
    {
        return await _context.AgentSkillExecutions
            .Where(e => e.CreateTime >= sinceUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AgentSkillExecution>> GetFailedSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken = default)
    {
        return await _context.AgentSkillExecutions
            .Where(e => e.CreateTime >= sinceUtc && !e.Success)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<AgentSkillExecution?> GetLastForUserAsync(string triggeredBy, DateTime sinceUtc, CancellationToken cancellationToken = default)
    {
        return await _context.AgentSkillExecutions
            .Where(e => e.TriggeredBy == triggeredBy && e.CreateTime >= sinceUtc)
            .OrderByDescending(e => e.CreateTime)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<AgentSkillExecution?> GetLastSuccessfulForUserAsync(string triggeredBy, DateTime sinceUtc, CancellationToken cancellationToken = default)
    {
        return await _context.AgentSkillExecutions
            .Where(e => e.TriggeredBy == triggeredBy && e.CreateTime >= sinceUtc && e.Success)
            .OrderByDescending(e => e.CreateTime)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
    }
}
