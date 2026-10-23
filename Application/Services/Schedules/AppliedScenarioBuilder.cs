// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// Builds the AnalyseScenario a wizard or harmonizer apply writes and the resource returned to the caller, so both
/// apply services create and describe their scenario identically.
/// </summary>
public static class AppliedScenarioBuilder
{
    /// <param name="name">Localized, per-group unique scenario name</param>
    /// <param name="groupId">Group the scenario belongs to; null = none</param>
    /// <param name="fromDate">First day of the scenario range</param>
    /// <param name="untilDate">Last day of the scenario range</param>
    /// <param name="token">Analyse token that tags every row of the scenario</param>
    /// <param name="runGroupId">Group of scenarios produced by one run chain</param>
    /// <param name="sourceToken">Token of the scenario the run planned on; null = the real schedule</param>
    public static AnalyseScenario Create(
        string name,
        Guid? groupId,
        DateOnly fromDate,
        DateOnly untilDate,
        Guid token,
        Guid runGroupId,
        Guid? sourceToken)
        => new()
        {
            Name = name,
            GroupId = groupId,
            FromDate = fromDate,
            UntilDate = untilDate,
            Token = token,
            RunGroupId = runGroupId,
            SourceToken = sourceToken,
        };

    /// <summary>
    /// Describes a freshly created scenario only; SubScoreJson, ChurnRatio and Stage0Violations are not set on the resource.
    /// </summary>
    public static AnalyseScenarioResource ToAppliedResource(AnalyseScenario scenario)
        => new()
        {
            Id = scenario.Id,
            Name = scenario.Name,
            Description = scenario.Description,
            GroupId = scenario.GroupId,
            FromDate = scenario.FromDate,
            UntilDate = scenario.UntilDate,
            Token = scenario.Token,
            RunGroupId = scenario.RunGroupId,
            CreatedByUser = scenario.CreatedByUser,
            Status = (int)scenario.Status,
        };
}
