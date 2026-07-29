// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF-backed repository for GoalCandidate. Implements GetByIdAsync, AddAsync, ExistsRecentAsync
/// (dedup lookup before a new candidate is persisted) and GetRecentAsync for the Phase 1 shadow-mode
/// reflection pipeline, plus UpdateAsync and GetForUserAsync for the Phase 2 read-only inbox.
/// </summary>

using Klacks.Api.Application.Services.Assistant.Planning;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class GoalCandidateRepository : IGoalCandidateRepository
{
    private readonly DataBaseContext _context;

    public GoalCandidateRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<GoalCandidate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.GoalCandidates
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task AddAsync(GoalCandidate candidate, CancellationToken cancellationToken = default)
    {
        await _context.GoalCandidates.AddAsync(candidate, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(GoalCandidate candidate, CancellationToken cancellationToken = default)
    {
        _context.GoalCandidates.Update(candidate);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsRecentAsync(string userId, string dedupHash, DateTime sinceUtc, CancellationToken cancellationToken = default)
    {
        return await _context.GoalCandidates
            .AsNoTracking()
            .AnyAsync(
                c => c.UserId == userId && c.DedupHash == dedupHash && c.CreateTime >= sinceUtc,
                cancellationToken);
    }

    public async Task<IReadOnlyList<GoalCandidate>> GetRecentAsync(string userId, int limit, CancellationToken cancellationToken = default)
    {
        return await _context.GoalCandidates
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreateTime)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GoalCandidate>> GetForUserAsync(string userId, string? status, int? take, CancellationToken cancellationToken = default)
    {
        var query = _context.GoalCandidates.Where(c => c.UserId == userId);

        query = status is null
            ? query.Where(c =>
                c.Status == GoalCandidateStatus.Shadow ||
                c.Status == GoalCandidateStatus.Proposed)
            : query.Where(c => c.Status == status);

        query = query.OrderByDescending(c => c.CreateTime);

        if (take is int limit && limit > 0)
        {
            query = query.Take(limit);
        }

        return await query.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<GoalCandidate?> GetByPlanIdAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        return await _context.GoalCandidates
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.PlanId == planId, cancellationToken);
    }

    // Restricted to candidates whose plan has not started yet, so the result set shrinks as plans run
    // instead of accumulating every candidate ever approved. Without this the oldest finished candidates
    // would fill the row limit forever and newer, genuinely retryable ones would never be reached.
    // The caller still re-checks the plan status before executing; this only keeps the sweep bounded.
    public async Task<IReadOnlyList<GoalCandidate>> GetApprovedWithPlanAsync(int limit, CancellationToken cancellationToken = default)
    {
        var draftingPlanIds = _context.AgentPlans
            .Where(p => p.Status == PlanStatus.Drafting)
            .Select(p => (Guid?)p.Id);

        return await _context.GoalCandidates
            .Where(c => c.Status == GoalCandidateStatus.Approved
                        && c.PlanId != null
                        && draftingPlanIds.Contains(c.PlanId))
            .OrderBy(c => c.CreateTime)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
