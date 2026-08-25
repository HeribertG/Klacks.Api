// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The scope a remediation scenario is cloned for. The window is supplied by the caller and never
/// derived from the condition here: AgentCondition.PayloadJson is free-form per detector kind, so only
/// the kind's own remediation knows which days its fix touches, and a generic parse would either guess
/// or force every payload into one shape.
/// </summary>
/// <param name="FromDate">First day the scenario covers.</param>
/// <param name="UntilDate">Last day the scenario covers, inclusive.</param>
/// <param name="Name">Scenario name shown to the planner. Null lets the service name it after the finding.</param>
/// <param name="GroupId">Group to clone. Null falls back to the condition's own group, which is the usual case.</param>

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record ConditionScenarioRequest(
    DateOnly FromDate,
    DateOnly UntilDate,
    string? Name = null,
    Guid? GroupId = null);
