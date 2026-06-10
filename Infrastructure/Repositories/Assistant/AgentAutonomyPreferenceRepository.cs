// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF-backed implementation of IAgentAutonomyPreferenceRepository.
/// </summary>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class AgentAutonomyPreferenceRepository : IAgentAutonomyPreferenceRepository
{
    private readonly DataBaseContext _context;

    public AgentAutonomyPreferenceRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<AgentAutonomyPreferenceRow?> GetAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.AgentAutonomyPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }

    public async Task<AgentAutonomyPreferenceRow> UpsertAsync(AgentAutonomyPreferenceRow row, CancellationToken cancellationToken = default)
    {
        var existing = await GetAsync(row.UserId, cancellationToken);
        if (existing == null)
        {
            row.Id = row.Id == Guid.Empty ? Guid.NewGuid() : row.Id;
            await _context.AgentAutonomyPreferences.AddAsync(row, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return row;
        }

        existing.Level = row.Level;
        await _context.SaveChangesAsync(cancellationToken);
        return existing;
    }
}
