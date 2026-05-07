// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Services.Schedules;

namespace Klacks.Api.Application.DTOs.Schedules.Wizard;

/// <summary>
/// Request to run a synchronous benchmark of the wizard (training/measurement use).
/// The request is scenario-isolated via AnalyseToken; no Work entities are persisted to the main scenario.
/// </summary>
/// <param name="PeriodFrom">Start date (inclusive).</param>
/// <param name="PeriodUntil">End date (inclusive).</param>
/// <param name="AgentIds">Clients to plan for.</param>
/// <param name="ShiftIds">Optional subset of shift definitions.</param>
/// <param name="AnalyseToken">Scenario isolation token (recommended: random per benchmark).</param>
/// <param name="TrainingOverrides">Tuning-parameter overrides for this benchmark.</param>
public sealed record WizardBenchmarkRequest(
    DateOnly PeriodFrom,
    DateOnly PeriodUntil,
    IReadOnlyList<Guid> AgentIds,
    IReadOnlyList<Guid>? ShiftIds,
    Guid? AnalyseToken,
    WizardTrainingOverrides? TrainingOverrides = null);
