// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// Result of the cover_absence flow: the isolated scenario holding the recorded absence (Break) and
/// the proposed replacements (Replacement WorkChanges), plus the slots that could not be covered.
/// The scenario is left for the user/agent to accept (accept_scenario) or discard (reject_scenario).
/// </summary>
/// <param name="ScenarioId">Id of the created AnalyseScenario</param>
/// <param name="Token">Scenario isolation token</param>
/// <param name="ScenarioName">Generated scenario name</param>
/// <param name="Covered">Slots with a proposed replacement</param>
/// <param name="Uncovered">Slots left uncovered (under-coverage or locked)</param>
public sealed record CoverAbsenceOutcome(
    Guid ScenarioId,
    Guid Token,
    string ScenarioName,
    IReadOnlyList<CoveredSlot> Covered,
    IReadOnlyList<UncoveredSlot> Uncovered);
