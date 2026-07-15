// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.Setup;

/// <summary>
/// One entry of compliance.counterRules / industryProfiles.*.counterRules (K18): count Event
/// occurrences per person in the calendar Period and warn/block once Threshold is reached. Event is
/// "nightShift", "workedDayInWeek" or "shiftExceedingHours"; Period is "week", "month" or "year"
/// (default: week for workedDayInWeek, year otherwise). HoursThreshold is required for — and only
/// allowed with — shiftExceedingHours.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class RegionSetupCounterRule
{
    public string? Event { get; set; }

    public string? Period { get; set; }

    public int? Threshold { get; set; }

    public decimal? HoursThreshold { get; set; }
}
