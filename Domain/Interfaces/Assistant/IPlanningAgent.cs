// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Phase 2 (autonomy roadmap) — Planning Agent contract.
/// Decomposes a user goal into a sequence of atomic skill-calls, plus paired verify-steps.
/// Returns an AgentPlan with serialized steps that the StepExecutor later runs one by one.
/// </summary>
/// <param name="goal">Free-text user goal (e.g. "plan the May 2026 schedule for Bern and apply it").</param>
/// <param name="userId">Owner of the plan.</param>
/// <param name="sessionId">Optional session this plan originated from.</param>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IPlanningAgent
{
    Task<AgentPlan> CreatePlanAsync(
        string goal,
        string userId,
        Guid? sessionId,
        CancellationToken cancellationToken = default);
}
