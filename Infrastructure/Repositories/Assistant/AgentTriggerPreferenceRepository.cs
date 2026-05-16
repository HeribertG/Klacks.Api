// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF-backed implementation of IAgentTriggerPreferenceRepository.
/// </summary>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class AgentTriggerPreferenceRepository : IAgentTriggerPreferenceRepository
{
    private readonly DataBaseContext _context;

    public AgentTriggerPreferenceRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<AgentTriggerPreferenceRow?> GetAsync(string userId, string triggerKind, CancellationToken cancellationToken = default)
    {
        return await _context.AgentTriggerPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.TriggerKind == triggerKind, cancellationToken);
    }

    public async Task<AgentTriggerPreferenceRow> UpsertAsync(AgentTriggerPreferenceRow row, CancellationToken cancellationToken = default)
    {
        var existing = await GetAsync(row.UserId, row.TriggerKind, cancellationToken);
        if (existing == null)
        {
            row.Id = row.Id == Guid.Empty ? Guid.NewGuid() : row.Id;
            await _context.AgentTriggerPreferences.AddAsync(row, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return row;
        }

        existing.Muted = row.Muted;
        existing.SnoozedUntilUtc = row.SnoozedUntilUtc;
        existing.MinimumSeverity = row.MinimumSeverity;
        await _context.SaveChangesAsync(cancellationToken);
        return existing;
    }
}
