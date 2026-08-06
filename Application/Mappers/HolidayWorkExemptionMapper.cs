// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Projects a holiday-work exemption onto its resource. Import identity is exposed read-only and never
/// travels the other way: a row created through the API is customer-owned.
/// </summary>

using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Application.Mappers;

public static class HolidayWorkExemptionMapper
{
    public static HolidayWorkExemptionResource ToResource(HolidayWorkExemptionRule rule) => new()
    {
        Id = rule.Id,
        Description = rule.Description,
        SchedulingRuleId = rule.SchedulingRuleId,
        ImportSourceKey = rule.ImportSourceKey,
    };
}
