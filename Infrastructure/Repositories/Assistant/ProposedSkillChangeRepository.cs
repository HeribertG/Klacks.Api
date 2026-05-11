// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core repository for proposed skill changes managed by the description-optimizer + approval flow.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class ProposedSkillChangeRepository : IProposedSkillChangeRepository
{
    private readonly DataBaseContext _context;

    public ProposedSkillChangeRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task AddAsync(ProposedSkillChange record, CancellationToken cancellationToken = default)
    {
        await _context.ProposedSkillChanges.AddAsync(record, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProposedSkillChange?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ProposedSkillChanges
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task UpdateAsync(ProposedSkillChange record, CancellationToken cancellationToken = default)
    {
        _context.ProposedSkillChanges.Update(record);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<ProposedSkillChange>> GetPendingAsync(int limit, CancellationToken cancellationToken = default)
    {
        return await _context.ProposedSkillChanges
            .Where(p => p.Status == ProposedChangeStatuses.Pending)
            .OrderByDescending(p => p.CreateTime)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasOpenProposalForSkillAsync(Guid skillId, string field, CancellationToken cancellationToken = default)
    {
        return await _context.ProposedSkillChanges
            .AnyAsync(p => p.SkillId == skillId
                        && p.Field == field
                        && p.Status == ProposedChangeStatuses.Pending,
                cancellationToken);
    }
}
