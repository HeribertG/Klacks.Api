// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for AgentPlan entities (Phase 2/3 of the autonomy roadmap).
/// Persists multi-step plans across sessions so the PlanStepExecutor can resume on HITL approval.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAgentPlanRepository
{
    Task<AgentPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(AgentPlan plan, CancellationToken cancellationToken = default);

    Task UpdateAsync(AgentPlan plan, CancellationToken cancellationToken = default);

    Task<List<AgentPlan>> ListByUserAsync(string userId, CancellationToken cancellationToken = default);
}
