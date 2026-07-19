// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules.HolisticHarmonizer;

/// <summary>
/// Response from the Holistic Harmonizer ApplyAsScenario endpoint.
/// </summary>
/// <param name="ScenarioId">Id of the newly created AnalyseScenario</param>
/// <param name="ScenarioToken">Unique token of the new scenario</param>
/// <param name="ScenarioName">Auto-generated name of the new scenario</param>
/// <param name="RunGroupId">Correlation id linking Wizard 1/2/3 scenarios from the same run</param>
/// <param name="CreatedWorkIds">Ids of Work entities written into the scenario</param>
/// <param name="ComplianceReport">End-state compliance diff of the new scenario versus the real plan</param>
public sealed record ApplyHolisticHarmonizerAsScenarioResponse(
    Guid ScenarioId,
    Guid ScenarioToken,
    string ScenarioName,
    Guid? RunGroupId,
    IReadOnlyList<Guid> CreatedWorkIds,
    ScenarioComplianceReport? ComplianceReport);
