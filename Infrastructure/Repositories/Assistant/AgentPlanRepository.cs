// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF-backed repository for AgentPlan. Implements GetByIdAsync, AddAsync, UpdateAsync
/// and ListByUserAsync for the PlanStepExecutor and Plan-Panel UI.
/// </summary>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class AgentPlanRepository : IAgentPlanRepository
{
    private readonly DataBaseContext _context;

    public AgentPlanRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<AgentPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AgentPlans
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task AddAsync(AgentPlan plan, CancellationToken cancellationToken = default)
    {
        await _context.AgentPlans.AddAsync(plan, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(AgentPlan plan, CancellationToken cancellationToken = default)
    {
        _context.AgentPlans.Update(plan);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<AgentPlan>> ListByUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.AgentPlans
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreateTime)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
