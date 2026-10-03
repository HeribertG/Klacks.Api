// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default <see cref="IPlanningConstraintRepository"/>. Reads filter soft-deleted rows explicitly; the period
/// query compares the nullable AnalyseToken with == against a nullable parameter, which EF translates to
/// null-safe equality (IS NOT DISTINCT FROM semantics), so the real plan never sees scenario constraints and
/// vice versa.
/// </summary>
/// <param name="context">Database context providing the PlanningConstraint DbSet</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Scheduling;

public class PlanningConstraintRepository : IPlanningConstraintRepository
{
    private readonly DataBaseContext _context;

    public PlanningConstraintRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<PlanningConstraint?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.PlanningConstraint
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);
    }

    public async Task<List<PlanningConstraint>> ListAsync(RuleApprovalStatus? status, CancellationToken cancellationToken = default)
    {
        var query = _context.PlanningConstraint.AsNoTracking().Where(c => !c.IsDeleted);
        if (status.HasValue)
        {
            query = query.Where(c => c.ApprovalStatus == status.Value);
        }

        return await query
            .OrderByDescending(c => c.CreateTime)
            .ThenBy(c => c.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PlanningConstraint>> GetApprovedForPeriodAsync(
        DateOnly from, DateOnly until, Guid? analyseToken, CancellationToken cancellationToken = default)
    {
        return await _context.PlanningConstraint
            .AsNoTracking()
            .Where(c => !c.IsDeleted
                && c.ApprovalStatus == RuleApprovalStatus.Approved
                && c.AnalyseToken == analyseToken
                && (c.ValidFrom == null || c.ValidFrom <= until)
                && (c.ValidUntil == null || c.ValidUntil >= from))
            .OrderBy(c => c.Id)
            .ToListAsync(cancellationToken);
    }

    public void Add(PlanningConstraint constraint)
    {
        _context.PlanningConstraint.Add(constraint);
    }

    public void Remove(PlanningConstraint constraint)
    {
        _context.PlanningConstraint.Remove(constraint);
    }

    public async Task<int> ExpireProposedAsync(DateTime createdBeforeUtc, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        return await _context.PlanningConstraint
            .Where(c => !c.IsDeleted
                && c.ApprovalStatus == RuleApprovalStatus.Proposed
                && c.CreateTime != null
                && c.CreateTime <= createdBeforeUtc)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(c => c.ApprovalStatus, RuleApprovalStatus.Rejected)
                    .SetProperty(c => c.UpdateTime, nowUtc)
                    .SetProperty(c => c.CurrentUserUpdated, PlanningConstraintDefaults.ExpirySweepActor),
                cancellationToken);
    }
}
