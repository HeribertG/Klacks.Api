// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persisted plan: a multi-step task that Klacksy decomposed into atomic skill-calls.
/// Used by the PlanningAgent (Phase 2 of the autonomy roadmap) so that long-running goals
/// survive across sessions and a step's verify can pause the plan for human approval.
/// </summary>
/// <param name="AgentId">Owning agent (currently only klacks-default).</param>
/// <param name="UserId">User who triggered the plan.</param>
/// <param name="SessionId">Optional originating session.</param>
/// <param name="Goal">Free-text user goal that produced this plan.</param>
/// <param name="StepsJson">Serialized list of PlanStep records (skill name + parameters + verify-skill).</param>
/// <param name="Status">drafting / executing / paused_for_approval / completed / aborted / failed.</param>
/// <param name="CurrentStepIndex">Zero-based index of the step currently executing or paused on.</param>
/// <param name="LastErrorMessage">Populated when status = failed/aborted, otherwise null.</param>
/// <param name="Origin">Where the plan came from; see AgentPlanOrigin. Defaults to UserGoal, since
/// every plan before Phase 3 of the self-directed-goals feature was created from a human-formulated
/// goal. A plan drafted from an approved GoalCandidate (self-reflection) is stamped SelfReflection so
/// Phase 4 can treat it differently from one a user started directly in chat or via the REST API.</param>

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Models.Assistant;

public class AgentPlan : BaseEntity
{
    public Guid AgentId { get; set; }

    public string? UserId { get; set; }

    public Guid? SessionId { get; set; }

    public string Goal { get; set; } = string.Empty;

    public string StepsJson { get; set; } = "[]";

    public string Status { get; set; } = "drafting";

    public int CurrentStepIndex { get; set; }

    public string? LastErrorMessage { get; set; }

    public string Origin { get; set; } = AgentPlanOrigin.UserGoal;
}
