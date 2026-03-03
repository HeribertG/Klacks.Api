// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class AgentRepository : IAgentRepository
{
    private readonly DataBaseContext _context;

    public AgentRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<Agent?> GetDefaultAgentAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Agents
            .Where(a => a.IsDefault && a.IsActive)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Agent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Agents
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<List<Agent>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Agents
            .OrderByDescending(a => a.IsDefault)
            .ThenBy(a => a.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Agent agent, CancellationToken cancellationToken = default)
    {
        await _context.Agents.AddAsync(agent, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Agent agent, CancellationToken cancellationToken = default)
    {
        _context.Agents.Update(agent);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
