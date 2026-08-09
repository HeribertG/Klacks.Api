// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Application.Interfaces.Schedules;

namespace Klacks.Api.Application.DTOs.Schedules.Wizard;

/// <summary>
/// Request to start a wizard run.
/// </summary>
/// <param name="PeriodFrom">Start date (inclusive).</param>
/// <param name="PeriodUntil">End date (inclusive).</param>
/// <param name="AgentIds">Clients to plan for.</param>
/// <param name="ShiftIds">Optional subset of shift definitions.</param>
/// <param name="AnalyseToken">Scenario isolation token (null = main scenario).</param>
/// <param name="TrainingOverrides">Optional tuning-parameter overrides for training/benchmark.</param>
/// <param name="AgentOrderIsUserDefined">True when AgentIds reflects an individual roster order arranged by the user; it is then used as the top-down priority order verbatim instead of being reshaped by guaranteed hours.</param>
/// <param name="ReplanFrom">Frozen-prefix replanning cut: every existing assignment of the period before this day stays exactly as it is (lifted into the run as locked work regardless of its lock level) and only the days from this day on are planned. Null = plan the whole period.</param>
public sealed record StartWizardRequest(
    DateOnly PeriodFrom,
    DateOnly PeriodUntil,
    IReadOnlyList<Guid> AgentIds,
    IReadOnlyList<Guid>? ShiftIds,
    Guid? AnalyseToken,
    WizardTrainingOverrides? TrainingOverrides = null,
    bool AgentOrderIsUserDefined = false,
    DateOnly? ReplanFrom = null);
