// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.ScheduleOptimizer.Models;

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// Builds a <see cref="CoreWizardContext"/> from the current DB state for a given wizard run.
/// All DB I/O happens here so the optimizer receives pure, in-memory data.
/// </summary>
public interface IWizardContextBuilder
{
    /// <summary>
    /// Assembles the full wizard context: agents, shifts, contract days (per date), schedule commands,
    /// shift preferences, break blockers and locked existing works.
    /// </summary>
    /// <param name="request">Period, group/agent scope, optional AnalyseToken for scenario isolation</param>
    /// <param name="ct">Cancellation token</param>
    Task<CoreWizardContext> BuildContextAsync(WizardContextRequest request, CancellationToken ct);
}

/// <summary>
/// Request parameters for building a wizard context.
/// </summary>
/// <param name="PeriodFrom">Start date (inclusive)</param>
/// <param name="PeriodUntil">End date (inclusive)</param>
/// <param name="AgentIds">Subset of agents to plan for (Client IDs)</param>
/// <param name="ShiftIds">Optional subset of shifts to consider; null = all shifts of the groups</param>
/// <param name="AnalyseToken">Scenario isolation token — propagates to all writes after apply</param>
/// <param name="TrainingOverrides">Optional per-run overrides for the TokenEvolution config (training/benchmark use)</param>
/// <param name="ContextDaysBefore">
/// How many days before <paramref name="PeriodFrom"/> the builder loads existing works, breaks and locked works as
/// boundary context. Boundary entries are placed in CoreWizardContext.Boundary* fields — never planned, never scored —
/// and let constraint validators detect MaxConsecutiveDays / MinRestHours runs that cross the start of the period.
/// Default 14 covers the longest realistic streak plus weekly caps.
/// </param>
/// <param name="ContextDaysAfter">Same as <paramref name="ContextDaysBefore"/>, applied after <paramref name="PeriodUntil"/>.</param>
/// <param name="AgentOrderIsUserDefined">
/// True when <paramref name="AgentIds"/> reflects an individual roster order the user arranged by hand —
/// that order is then used as the top-down priority order unchanged. False (default) means the order is the
/// regular list sort and the builder reshapes it by contractually guaranteed hours (descending, stable).
/// </param>
public sealed record WizardContextRequest(
    DateOnly PeriodFrom,
    DateOnly PeriodUntil,
    IReadOnlyList<Guid> AgentIds,
    IReadOnlyList<Guid>? ShiftIds,
    Guid? AnalyseToken,
    WizardTrainingOverrides? TrainingOverrides = null,
    int ContextDaysBefore = 14,
    int ContextDaysAfter = 14,
    bool AgentOrderIsUserDefined = false);
