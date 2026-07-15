// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.Setup;

/// <summary>
/// One entry of compliance.restDayRotations (K10): at least MinFree occurrences of DayOfWeek (English
/// day name, e.g. "sunday") must be work-free within any trailing window of WindowWeeks occurrences.
/// All three fields are required; MinFree must not exceed WindowWeeks.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class RegionSetupRestDayRotation
{
    public string? DayOfWeek { get; set; }

    public int? MinFree { get; set; }

    public int? WindowWeeks { get; set; }
}
