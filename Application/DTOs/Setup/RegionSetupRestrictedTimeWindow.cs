// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.Setup;

/// <summary>
/// One entry of compliance.restrictedTimeWindows (K16): forbids work between DailyStart and DailyEnd on
/// every calendar day inside the season SeasonFrom..SeasonTo (both "MM-DD"), for shifts tagged via
/// AppliesToGroupTag (a Group.Name; empty/absent = all shifts). SeasonFrom/SeasonTo months and days and the
/// two times are parsed and validated on import; a DailyEnd not greater than DailyStart wraps past
/// midnight, and DailyEnd equal to DailyStart means a full 24 hour ban window.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class RegionSetupRestrictedTimeWindow
{
    public string? SeasonFrom { get; set; }

    public string? SeasonTo { get; set; }

    public string? DailyStart { get; set; }

    public string? DailyEnd { get; set; }

    public string? AppliesToGroupTag { get; set; }
}
