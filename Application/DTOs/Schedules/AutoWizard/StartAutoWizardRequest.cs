// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Constants;

namespace Klacks.Api.Application.DTOs.Schedules.AutoWizard;

/// <param name="PeriodFrom">Start date (inclusive) of the schedule window to optimise.</param>
/// <param name="PeriodUntil">End date (inclusive) of the schedule window to optimise.</param>
/// <param name="AgentIds">Clients whose rows are part of the run.</param>
/// <param name="ShiftIds">Shifts visible in the current schedule view; null/empty falls back to all shifts in the date range (slow on large databases).</param>
/// <param name="GroupId">Optional group scope for scenario cloning and name uniqueness.</param>
/// <param name="AnalyseToken">Source-scenario isolation token; null = main scenario.</param>
/// <param name="Language">Optional UI language for the LLM stage (e.g. "en", "de"); null = engine default.</param>
/// <param name="ContextDaysBefore">
/// Boundary context days propagated to all three stages so validators can detect MaxConsecutive / MinRest runs
/// crossing the period start. Default 14. Override with 0 to disable.
/// </param>
/// <param name="ContextDaysAfter">Same as <paramref name="ContextDaysBefore"/>, after <paramref name="PeriodUntil"/>.</param>
/// <param name="AgentOrderIsUserDefined">True when AgentIds reflects an individual roster order arranged by the user; stage 1 then uses it as the top-down priority order verbatim instead of reshaping by guaranteed hours.</param>
public sealed record StartAutoWizardRequest(
    DateOnly PeriodFrom,
    DateOnly PeriodUntil,
    IReadOnlyList<Guid> AgentIds,
    IReadOnlyList<Guid>? ShiftIds,
    Guid? GroupId,
    Guid? AnalyseToken,
    string? Language,
    int ContextDaysBefore = ScenarioConstants.BoundaryDays,
    int ContextDaysAfter = ScenarioConstants.BoundaryDays,
    bool AgentOrderIsUserDefined = false);
