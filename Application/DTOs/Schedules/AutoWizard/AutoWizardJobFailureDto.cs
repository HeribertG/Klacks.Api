// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules.AutoWizard;

/// <summary>
/// Reported when the AutoWizard chain did not finish. A chain that failed in a later stage may still
/// have produced a usable scenario in an earlier one - after minutes of compute that result is worth
/// handing to the operator instead of discarding it silently.
/// </summary>
/// <param name="JobId">The orchestrator job.</param>
/// <param name="FailedStage">Stage the chain stopped in (Wizard, Harmonizer, HolisticHarmonizer).</param>
/// <param name="Reason">Why it stopped, in the stage's own words where available.</param>
/// <param name="PartialScenarioId">Id of the last scenario that was produced, or null.</param>
/// <param name="PartialScenarioToken">Token of that scenario, or null.</param>
/// <param name="PartialScenarioName">Name of that scenario, or null.</param>
public sealed record AutoWizardJobFailureDto(
    Guid JobId,
    string FailedStage,
    string Reason,
    Guid? PartialScenarioId,
    Guid? PartialScenarioToken,
    string? PartialScenarioName);
