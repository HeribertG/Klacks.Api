// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core repository for SkillSelectionTrajectory used to capture per-turn skill selection telemetry.
/// </summary>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class SkillSelectionTrajectoryRepository : ISkillSelectionTrajectoryRepository
{
    private readonly DataBaseContext _context;

    public SkillSelectionTrajectoryRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task AddAsync(SkillSelectionTrajectory record, CancellationToken cancellationToken = default)
    {
        await _context.SkillSelectionTrajectories.AddAsync(record, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<SkillSelectionTrajectory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SkillSelectionTrajectories
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task UpdateAsync(SkillSelectionTrajectory record, CancellationToken cancellationToken = default)
    {
        _context.SkillSelectionTrajectories.Update(record);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<SkillSelectionTrajectory>> GetRecentAsync(Guid agentId, int limit, CancellationToken cancellationToken = default)
    {
        return await _context.SkillSelectionTrajectories
            .Where(t => t.AgentId == agentId)
            .OrderByDescending(t => t.CreateTime)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SkillSelectionTrajectory>> GetCorrectedAsync(Guid agentId, int limit, CancellationToken cancellationToken = default)
    {
        return await _context.SkillSelectionTrajectories
            .Where(t => t.AgentId == agentId && t.WasCorrected)
            .OrderByDescending(t => t.CreateTime)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<SkillSelectionTrajectory?> FindMostRecentByUserAndHashAsync(string userId, string userMessageHash, CancellationToken cancellationToken = default)
    {
        return await _context.SkillSelectionTrajectories
            .Where(t => t.UserId == userId && t.UserMessageHash == userMessageHash)
            .OrderByDescending(t => t.CreateTime)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
