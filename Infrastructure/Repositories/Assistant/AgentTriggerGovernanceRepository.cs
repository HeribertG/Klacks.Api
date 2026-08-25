// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core implementation of the proactive governance rule store. Self-committing: every method that
/// writes calls SaveChangesAsync itself, because its callers run outside the HTTP request cycle.
/// </summary>
/// <param name="context">The shared database context.</param>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class AgentTriggerGovernanceRepository : IAgentTriggerGovernanceRepository
{
    private readonly DataBaseContext _context;

    public AgentTriggerGovernanceRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AgentTriggerGovernance>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.AgentTriggerGovernances
            .AsNoTracking()
            .OrderBy(rule => rule.TriggerKind)
            .ThenBy(rule => rule.GroupId)
            .ToListAsync(cancellationToken);
    }

    public async Task<AgentTriggerGovernance?> FindAsync(
        string triggerKind, Guid? groupId, CancellationToken cancellationToken)
    {
        return await _context.AgentTriggerGovernances
            .AsNoTracking()
            .FirstOrDefaultAsync(
                rule => rule.TriggerKind == triggerKind && rule.GroupId == groupId,
                cancellationToken);
    }

    public async Task<AgentTriggerGovernance> UpsertAsync(
        AgentTriggerGovernance governance, CancellationToken cancellationToken)
    {
        var existing = await _context.AgentTriggerGovernances
            .FirstOrDefaultAsync(
                rule => rule.TriggerKind == governance.TriggerKind && rule.GroupId == governance.GroupId,
                cancellationToken);

        if (existing is null)
        {
            await _context.AgentTriggerGovernances.AddAsync(governance, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return governance;
        }

        existing.MaxAction = governance.MaxAction;
        existing.Enabled = governance.Enabled;
        existing.ResponsibleOwnerUserId = governance.ResponsibleOwnerUserId;
        existing.DailyActionBudget = governance.DailyActionBudget;
        existing.WindowActionLimit = governance.WindowActionLimit;
        existing.WindowMinutes = governance.WindowMinutes;

        await _context.SaveChangesAsync(cancellationToken);
        return existing;
    }
}
