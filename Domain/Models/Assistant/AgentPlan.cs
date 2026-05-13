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

using Klacks.Api.Domain.Common;

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
}
