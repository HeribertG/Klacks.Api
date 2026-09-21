// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core implementation of the standing-approval store. Stage-only: nothing here calls
/// SaveChangesAsync, the grant and revoke handlers commit through IUnitOfWork.
/// </summary>
/// <param name="context">The shared database context.</param>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class StandingApprovalRepository : IStandingApprovalRepository
{
    private readonly DataBaseContext _context;

    public StandingApprovalRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<StandingApproval?> FindActiveAsync(
        string triggerKind,
        Guid? groupId,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        return await _context.StandingApprovals
            .AsNoTracking()
            .Where(approval => approval.TriggerKind == triggerKind && approval.GroupId == groupId)
            .Where(StandingApprovalPolicy.ActiveAt(nowUtc))
            .OrderByDescending(approval => approval.GrantedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StandingApproval>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.StandingApprovals
            .AsNoTracking()
            .OrderByDescending(approval => approval.GrantedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<StandingApproval?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.StandingApprovals
            .AsNoTracking()
            .FirstOrDefaultAsync(approval => approval.Id == id, cancellationToken);
    }

    public async Task AddAsync(StandingApproval approval, CancellationToken cancellationToken = default)
    {
        await _context.StandingApprovals.AddAsync(approval, cancellationToken);
    }

    public async Task<bool> TryRevokeAsync(
        Guid id,
        Guid revokedByUserId,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var stored = await _context.StandingApprovals
            .FirstOrDefaultAsync(approval => approval.Id == id && approval.RevokedAtUtc == null, cancellationToken);

        if (stored is null)
        {
            return false;
        }

        stored.RevokedAtUtc = revokedAtUtc;
        stored.RevokedByUserId = revokedByUserId;

        return true;
    }
}
