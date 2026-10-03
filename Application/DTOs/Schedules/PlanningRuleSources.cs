// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// Which persisted rule families a planning-rule load returns. The post-hoc validators report CounterRule
/// through CounterRuleEvaluator and must not load (or count) it a second time.
/// </summary>
[Flags]
public enum PlanningRuleSources
{
    CounterRules = 1,
    PlanningConstraints = 2,
    All = CounterRules | PlanningConstraints,
}
