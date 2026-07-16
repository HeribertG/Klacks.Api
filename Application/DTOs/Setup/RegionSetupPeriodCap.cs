// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.Setup;

/// <summary>
/// One entry of compliance.periodCaps. Two mutually exclusive modes: a fixed-period cap needs
/// Period/Scope/CapHours; a K6 rolling-average cap needs WindowWeeks/MaxAverageWeeklyHours. An entry
/// must populate exactly one of the two field groups — RegionSetupService validates this and rejects
/// entries that mix or omit both. Within fixed-period mode, Period "customWeeks" caps hours over a
/// trailing window of CustomPeriodWeeks weeks ending on the evaluated day instead of a calendar-anchored
/// month/quarter/year; CustomPeriodWeeks is required for (and only allowed with) that period value.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class RegionSetupPeriodCap
{
    public string? Period { get; set; }

    public string? Scope { get; set; }

    public decimal? CapHours { get; set; }

    public int? WarnAtPercent { get; set; }

    public int? CustomPeriodWeeks { get; set; }

    public int? WindowWeeks { get; set; }

    public decimal? MaxAverageWeeklyHours { get; set; }
}
