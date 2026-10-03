// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Everything a rule consumer needs to build a RuleEvaluationContext from the database: the approved rules,
/// the agents with their contractual night window and workload, and the carry-in segments outside the
/// period that the caller's own boundary does not already cover.
/// </summary>
/// <param name="Rules">Approved planning rules (CounterRule and PlanningConstraint), deterministic order</param>
/// <param name="Agents">One RuleAgent per requested agent, in request order</param>
/// <param name="CarryIn">Worked segments outside [from - coveredBoundaryDays, until + coveredBoundaryDays]</param>

using Klacks.ScheduleOptimizer.Constraints.Rules;

namespace Klacks.Api.Application.DTOs.Schedules;

public sealed record PlanningRuleSet(
    IReadOnlyList<PlanRule> Rules,
    IReadOnlyList<RuleAgent> Agents,
    IReadOnlyList<RuleSegment> CarryIn);
