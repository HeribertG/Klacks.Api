// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Single loader of the planning rules every consumer evaluates (Wizard, Harmonizer, plan checks): it unites
/// the approved CounterRule rows (as PeriodCountRule) and the approved PlanningConstraint rows valid in the
/// period and resolves every scope into the agent-id set of the requested agents. Proposed, Rejected and
/// Revoked rows are never returned.
/// </summary>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.ScheduleOptimizer.Constraints.Rules;

namespace Klacks.Api.Application.Interfaces.Schedules;

public interface IPlanningRuleSetLoader
{
    /// <param name="agentIds">Client ids of the agents being planned or checked</param>
    /// <param name="from">First day of the period (inclusive)</param>
    /// <param name="until">Last day of the period (inclusive)</param>
    /// <param name="analyseToken">Scenario token; null for the real plan</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<IReadOnlyList<PlanRule>> LoadAsync(
        IReadOnlyCollection<Guid> agentIds,
        DateOnly from,
        DateOnly until,
        Guid? analyseToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rules plus agents (night window, workload) plus carry-in. <paramref name="coveredBoundaryDays"/> is the
    /// number of days on each side of the period the caller's boundary already holds (engine ContextDays; 0
    /// for a consumer without an engine boundary). Carry-in segments are returned ONLY outside that window, so
    /// appending them to the engine boundary never counts a day twice.
    /// </summary>
    Task<PlanningRuleSet> LoadRuleSetAsync(
        IReadOnlyCollection<Guid> agentIds,
        DateOnly from,
        DateOnly until,
        Guid? analyseToken,
        int coveredBoundaryDays,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <see cref="LoadRuleSetAsync(IReadOnlyCollection{Guid}, DateOnly, DateOnly, Guid?, int, CancellationToken)"/>
    /// restricted to the rule families in <paramref name="sources"/>; the carry-in is sized for the returned rules only.
    /// </summary>
    Task<PlanningRuleSet> LoadRuleSetAsync(
        IReadOnlyCollection<Guid> agentIds,
        DateOnly from,
        DateOnly until,
        Guid? analyseToken,
        int coveredBoundaryDays,
        PlanningRuleSources sources,
        CancellationToken cancellationToken = default);
}
