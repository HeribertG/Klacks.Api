// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Notifications;

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// Result of materializing proposed placements into an isolated AnalyseScenario: which placements
/// were written, which were rejected (with reason) and the non-blocking warnings on the written set.
/// The scenario is left for the user/agent to accept (accept_scenario) or discard (reject_scenario).
/// </summary>
/// <param name="ScenarioId">Id of the created AnalyseScenario</param>
/// <param name="Token">Scenario isolation token tagging the written works</param>
/// <param name="ScenarioName">Generated scenario name</param>
/// <param name="Written">Placements written into the scenario</param>
/// <param name="Rejected">Placements skipped, with reason</param>
/// <param name="Warnings">Non-blocking schedule warnings on the written set (rest, overtime, ...)</param>
public sealed record ProposePlanOutcome(
    Guid ScenarioId,
    Guid Token,
    string ScenarioName,
    IReadOnlyList<PlacementInput> Written,
    IReadOnlyList<RejectedPlacement> Rejected,
    IReadOnlyList<ScheduleValidationNotificationDto> Warnings);
