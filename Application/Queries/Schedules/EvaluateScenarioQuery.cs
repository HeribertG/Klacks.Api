// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Schedules;

/// <summary>
/// Evaluates one AnalyseScenario on demand from its persisted data: rule-compliance (via the same
/// engine as detect_conflicts) plus the change-set it introduces over the real plan. At least one
/// of ScenarioId / Token must be set; Token wins when both are supplied.
/// </summary>
/// <param name="ScenarioId">Id of the scenario to evaluate (from list_scenarios)</param>
/// <param name="Token">Isolation token of the scenario to evaluate</param>
public record EvaluateScenarioQuery(
    Guid? ScenarioId,
    Guid? Token) : IRequest<ScenarioEvaluationResult>;
