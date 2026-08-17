// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for AgentPlan entities (Phase 2/3 of the autonomy roadmap).
/// Persists multi-step plans across sessions so the PlanStepExecutor can resume on HITL approval.
/// </summary>
/// <remarks>
/// <see cref="AddAsync"/> and <see cref="UpdateAsync"/> commit immediately via <c>SaveChangesAsync</c> —
/// this is the self-committing repository convention (not the stage-only + IUnitOfWork one used by
/// core-domain repositories), chosen because most callers (PlanStepExecutor, background services,
/// skills) run outside the HTTP request cycle and each plan state transition must persist on its own.
/// Never call this repository between a stage-only repository write and its IUnitOfWork.CompleteAsync() —
/// the SaveChangesAsync here flushes the whole shared DbContext, including that pending write.
/// </remarks>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAgentPlanRepository
{
    Task<AgentPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Persists the new plan immediately (commits its own SaveChangesAsync).</summary>
    Task AddAsync(AgentPlan plan, CancellationToken cancellationToken = default);

    /// <summary>Persists the updated plan immediately (commits its own SaveChangesAsync).</summary>
    Task UpdateAsync(AgentPlan plan, CancellationToken cancellationToken = default);

    Task<List<AgentPlan>> ListByUserAsync(string userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AgentPlan>> GetStalePausedForApprovalAsync(
        DateTime updatedBeforeUtc, int maxRows, CancellationToken cancellationToken = default);
}
