// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared plan lifecycle used by both the AgentPlansController and the create_plan chat skill so the
/// create-and-start path lives in one place. CreatePlanAsync decomposes a goal and persists the plan
/// WITHOUT running it; ResolveExecutionProviderAsync attributes the execution to the default model's
/// provider; StartBackgroundExecution kicks off the fire-and-forget executor tracked in the shared
/// IPlanExecutionRegistry so an abort can cancel it cooperatively.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Planning;

public interface IPlanChatService
{
    Task<AgentPlan> CreatePlanAsync(
        string goal,
        string userId,
        Guid? sessionId,
        CancellationToken cancellationToken = default,
        string origin = AgentPlanOrigin.UserGoal);

    Task<PlanProviderResolution> ResolveExecutionProviderAsync(CancellationToken cancellationToken = default);

    void StartBackgroundExecution(Guid planId, SkillExecutionContext skillContext, bool resume);
}
