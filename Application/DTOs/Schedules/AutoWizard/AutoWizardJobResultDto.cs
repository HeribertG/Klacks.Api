// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.DTOs.Schedules.AutoWizard;

/// <summary>
/// Final completion payload for the AutoWizard orchestrator. Intentionally minimal — the user
/// only needs to know "the schedule is ready" plus a pointer to the resulting analyse-scenario
/// (the Holistic Harmonizer output) so the UI can switch the schedule view to it.
/// </summary>
/// <param name="JobId">Orchestrator job id (matches the JobId returned from /Start).</param>
/// <param name="FinalScenarioId">Id of the analyse-scenario produced by the final stage, or null if no scenario was created.</param>
/// <param name="FinalScenarioToken">Token of the analyse-scenario produced by the final stage, or null.</param>
/// <param name="FinalScenarioName">Display name of the analyse-scenario produced by the final stage, or null.</param>
/// <param name="ElapsedMs">Total wall-clock duration of the orchestration in milliseconds.</param>
/// <param name="QualificationGaps">Assignments in the final plan whose agent lacks a required mandatory qualification.</param>
public sealed record AutoWizardJobResultDto(
    Guid JobId,
    Guid? FinalScenarioId,
    Guid? FinalScenarioToken,
    string? FinalScenarioName,
    long ElapsedMs,
    IReadOnlyList<QualificationGapDetail> QualificationGaps);
