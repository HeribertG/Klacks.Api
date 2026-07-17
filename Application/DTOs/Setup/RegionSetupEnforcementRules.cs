// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.Setup;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class RegionSetupEnforcementRules
{
    public string? MaxDailyHours { get; set; }

    public string? MaxWeeklyHours { get; set; }

    public string? MinRestHours { get; set; }

    public string? MinRestDays { get; set; }

    public string? MaxConsecutiveDays { get; set; }

    public string? PeriodCap { get; set; }

    public string? RollingAverage { get; set; }

    public string? RestDayRotation { get; set; }

    public string? CounterRule { get; set; }

    public string? CompensatoryRest { get; set; }
}
