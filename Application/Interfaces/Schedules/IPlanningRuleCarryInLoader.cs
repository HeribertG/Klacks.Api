// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Loads the persisted worked segments a rule set reads outside the planning period (the rest of every
/// counted week, month or year and the neighbour days of the sequence rules), minus the window the caller's
/// boundary already covers. The returned segments go into the carryIn parameter of
/// RuleEvaluationContextFactory, which appends them to the engine boundary unchanged.
/// </summary>

using Klacks.ScheduleOptimizer.Constraints.Rules;

namespace Klacks.Api.Application.Interfaces.Schedules;

public interface IPlanningRuleCarryInLoader
{
    /// <param name="agentIds">Agents to load segments for</param>
    /// <param name="from">First day of the period (inclusive)</param>
    /// <param name="until">Last day of the period (inclusive)</param>
    /// <param name="rules">Rule set that decides how far outside the period to read</param>
    /// <param name="analyseToken">Scenario token; null for the real plan</param>
    /// <param name="coveredBoundaryDays">Days on each side of the period the caller already holds</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<IReadOnlyList<RuleSegment>> LoadAsync(
        IReadOnlyCollection<Guid> agentIds,
        DateOnly from,
        DateOnly until,
        IReadOnlyList<PlanRule> rules,
        Guid? analyseToken,
        int coveredBoundaryDays,
        CancellationToken cancellationToken = default);
}
