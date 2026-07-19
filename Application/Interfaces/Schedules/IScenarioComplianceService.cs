// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

/// <summary>
/// End-state compliance verdict for an isolated scenario: replays the period validation engine over
/// the scenario world and the real world, diffs away pre-existing real-plan issues and reports what
/// the scenario NEWLY introduces, with the Block-mode enforced errors split out as blocking. Built as
/// the acceptance gate foundation (accept_scenario) so the same engine that closes a period decides
/// whether a scenario may be accepted.
/// </summary>
public interface IScenarioComplianceService
{
    Task<ScenarioComplianceReport> EvaluateAsync(
        DateOnly from,
        DateOnly until,
        Guid? groupId,
        Guid analyseToken,
        CancellationToken cancellationToken);
}
