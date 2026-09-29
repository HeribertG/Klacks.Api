// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Shared plan lifecycle used by both the AgentPlansController and the create_plan chat skill so the
/// create-and-start path lives in one place. DraftPlanAsync decomposes a goal WITHOUT persisting or
/// running it, so callers inspect the drafted steps first and persist only a plan that has at least one;
/// ResolveExecutionProviderAsync attributes the execution to the default model's provider; StartBackgroundExecution kicks off the fire-and-forget executor tracked in the shared
/// IPlanExecutionRegistry so an abort can cancel it cooperatively.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Planning;

public interface IPlanChatService
{
    Task<AgentPlan> DraftPlanAsync(
        string goal,
        string userId,
        Guid? sessionId,
        CancellationToken cancellationToken = default,
        string origin = AgentPlanOrigin.UserGoal);

    Task<PlanProviderResolution> ResolveExecutionProviderAsync(CancellationToken cancellationToken = default);

    void StartBackgroundExecution(Guid planId, SkillExecutionContext skillContext, bool resume);
}
