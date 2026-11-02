// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Builds the ScenarioVariantResource of a variant (key) row; the single place that decides IsExpired.
/// </summary>
/// <param name="variant">Variant row to describe</param>
/// <param name="shiftCount">Number of services (SealedOrder shifts) the variant carries</param>
/// <param name="today">Today in the company's time zone; a variant whose period ended before it is expired</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Services.ScenarioVariants;

public static class ScenarioVariantResourceFactory
{
    public static ScenarioVariantResource Create(AnalyseScenario variant, int shiftCount, DateOnly today)
    {
        return new ScenarioVariantResource
        {
            Id = variant.Id,
            Name = variant.Name,
            Description = variant.Description,
            GroupId = variant.GroupId ?? Guid.Empty,
            FromDate = variant.FromDate,
            UntilDate = variant.UntilDate,
            Token = variant.Token,
            Status = (int)variant.Status,
            IsExpired = variant.UntilDate < today,
            ShiftCount = shiftCount
        };
    }
}
