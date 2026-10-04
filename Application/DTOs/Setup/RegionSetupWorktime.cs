// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.Setup;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class RegionSetupWorktime
{
    public decimal? MaximumHours { get; set; }

    public decimal? MinimumHours { get; set; }

    public decimal? FullTime { get; set; }

    public decimal? GuaranteedHours { get; set; }

    public decimal? DefaultWorkingHours { get; set; }

    public decimal? OvertimeThreshold { get; set; }

    public decimal? VacationDaysPerYear { get; set; }

    public decimal? MaxDailyHours { get; set; }

    public decimal? MaxWeeklyHours { get; set; }

    public decimal? MaxConsecutiveDays { get; set; }

    public decimal? MinRestDays { get; set; }

    public decimal? MinPauseHours { get; set; }

    public decimal? MaxDailySpanHours { get; set; }
}
