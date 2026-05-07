// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules.Wizard;

/// <summary>
/// Response from ApplyAsScenario containing the created scenario and work ids.
/// </summary>
/// <param name="ScenarioId">Id of the newly created AnalyseScenario.</param>
/// <param name="ScenarioToken">Unique token of the new scenario.</param>
/// <param name="ScenarioName">Auto-generated name of the new scenario.</param>
/// <param name="RunGroupId">Correlation id linking Wizard 1, Wizard 2 and Holistic Harmonizer scenarios from the same test run.</param>
/// <param name="CreatedWorkIds">Ids of Work entities written into the scenario.</param>
public sealed record ApplyAsScenarioResponse(
    Guid ScenarioId,
    Guid ScenarioToken,
    string ScenarioName,
    Guid? RunGroupId,
    IReadOnlyList<Guid> CreatedWorkIds);
