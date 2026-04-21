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
public sealed record WizardContextRequest(
    DateOnly PeriodFrom,
    DateOnly PeriodUntil,
    IReadOnlyList<Guid> AgentIds,
    IReadOnlyList<Guid>? ShiftIds,
    Guid? AnalyseToken);
