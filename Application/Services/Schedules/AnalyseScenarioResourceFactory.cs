// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Builds the AnalyseScenarioResource from a scenario row, so create, get, list, rename and the apply services describe a
/// scenario identically.
/// </summary>
/// <param name="scenario">The scenario row</param>
/// <param name="variantIds">Variants (keys) the scenario carries</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

public static class AnalyseScenarioResourceFactory
{
    public static AnalyseScenarioResource ToResource(AnalyseScenario scenario, IReadOnlyList<Guid> variantIds)
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
            SubScoreJson = scenario.SubScoreJson,
            ChurnRatio = scenario.ChurnRatio,
            Stage0Violations = scenario.Stage0Violations,
            VariantIds = variantIds,
        };
}
