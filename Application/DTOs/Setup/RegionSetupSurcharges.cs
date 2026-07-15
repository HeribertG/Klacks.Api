// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.Setup;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class RegionSetupSurcharges
{
    public decimal? NightRate { get; set; }

    public decimal? HolidayRate { get; set; }

    public decimal? We1Rate { get; set; }

    public decimal? We2Rate { get; set; }

    public decimal? We3Rate { get; set; }

    public RegionSetupNightWindow? NightWindow { get; set; }

    /// <summary>
    /// Per surcharge-type rate interpretation, keyed by "night"/"holiday"/"weekend1"/"weekend2"/"weekend3".
    /// Value must be "multiplier" (default), "fixedPerHour" or "fixedPerShift".
    /// </summary>
    public Dictionary<string, string>? RateModes { get; set; }

    /// <summary>
    /// Optional per surcharge-type minimum amount per hour, keyed the same way as <see cref="RateModes"/>,
    /// used as the floor in the combi mode max(Multiplier result, MinimumPerHour * hours).
    /// </summary>
    public Dictionary<string, decimal>? MinimumsPerHour { get; set; }

    /// <summary>
    /// K4: how the K3 overtime surcharge combines with this section's Night/Weekend/Holiday result.
    /// Must be "highestWins" (default — today's behavior, unchanged for every installation that never
    /// sets this) or "additive".
    /// </summary>
    public string? StackingMode { get; set; }

    /// <summary>
    /// K3: the overtime tier configuration. Absent (default) means overtime surcharges stay disabled.
    /// </summary>
    public RegionSetupOvertime? Overtime { get; set; }
}
