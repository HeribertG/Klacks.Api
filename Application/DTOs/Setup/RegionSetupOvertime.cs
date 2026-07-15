// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.Setup;

/// <summary>
/// K3 overtime tier configuration. Basis must be "day" or "week" (default "day"); RateMode must be
/// "multiplier" or "fixedPerHour" (default "multiplier") — "fixedPerShift" is rejected, a flat per-shift
/// amount cannot be split across tiers by hours worked. Tiers supports at most 3 entries (Overtime1/2/3),
/// strictly ascending by AfterHours.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class RegionSetupOvertime
{
    public string? Basis { get; set; }

    public string? RateMode { get; set; }

    public List<RegionSetupOvertimeTier>? Tiers { get; set; }
}
