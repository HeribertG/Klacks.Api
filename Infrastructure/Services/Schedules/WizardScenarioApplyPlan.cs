// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.ScheduleOptimizer.Models;

namespace Klacks.Api.Infrastructure.Services.Schedules;

/// <summary>
/// Everything a Wizard-1 scenario apply decides before it opens its transaction.
/// </summary>
/// <param name="Items">Non-locked tokens of the cached result</param>
/// <param name="GroupId">Group of the new scenario after inheriting the source scenario's group</param>
/// <param name="Name">Localized, per-group unique scenario name</param>
/// <param name="Token">Analyse token of the new scenario</param>
/// <param name="RunGroupId">Group of scenarios produced by one run chain</param>
/// <param name="SourceAnalyseToken">Token of the scenario the run planned on; null = the real schedule</param>
/// <param name="PeriodFrom">First day the planner placed a token on</param>
/// <param name="PeriodUntil">Last day the planner placed a token on</param>
/// <param name="ScenarioFrom">First day of the new scenario, widened to the source scenario's range</param>
/// <param name="ScenarioUntil">Last day of the new scenario, widened to the source scenario's range</param>
public sealed record WizardScenarioApplyPlan(
    IReadOnlyList<CoreToken> Items,
    Guid? GroupId,
    string Name,
    Guid Token,
    Guid RunGroupId,
    Guid? SourceAnalyseToken,
    DateOnly PeriodFrom,
    DateOnly PeriodUntil,
    DateOnly ScenarioFrom,
    DateOnly ScenarioUntil);
