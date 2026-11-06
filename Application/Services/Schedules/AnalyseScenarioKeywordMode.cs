// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;
using Klacks.ScheduleOptimizer.Models;

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// Reads the keyword mode a scenario was planned in from its persisted flag (AnalyseScenario.SoftKeywordPlan); the domain
/// entity carries a flag because the domain layer may not reference the optimizer's KeywordMode.
/// </summary>
public static class AnalyseScenarioKeywordMode
{
    /// <summary>Soft when the scenario was planned with the schedule keywords as wishes, otherwise Hard.</summary>
    /// <param name="scenario">The scenario row.</param>
    public static KeywordMode Of(AnalyseScenario scenario)
        => scenario.SoftKeywordPlan ? KeywordMode.Soft : KeywordMode.Hard;
}
