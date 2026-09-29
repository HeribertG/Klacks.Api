// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Phase 3 of the self-directed-goals roadmap: drafts an AgentPlan from an approved GoalCandidate so
/// a human can see the steps before anyone talks about running them. Never executes a step — only
/// IPlanChatService.DraftPlanAsync is called, and the plan is persisted in status "drafting" only if it has steps.
/// </summary>
/// <param name="candidateId">Id of the GoalCandidate to draft a plan for.</param>

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IGoalPlanDraftService
{
    Task<Guid?> DraftForCandidateAsync(Guid candidateId, CancellationToken cancellationToken = default);
}
