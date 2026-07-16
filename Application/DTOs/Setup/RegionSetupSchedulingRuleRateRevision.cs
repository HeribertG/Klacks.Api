// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.Setup;

/// <summary>
/// One dated full-snapshot revision of a SchedulingRule preset's surcharge rates and overtime tiers
/// inside an industry profile (K20). ValidFrom (ISO yyyy-MM-dd) is required and, combined with the parent
/// preset key, forms the import natural key. Each revision is a FULL snapshot: an omitted rate — and an
/// omitted overtime block — does NOT inherit from the base rule or an earlier revision but falls through
/// to the contract/settings chain, so an author who wants to keep a rate or the overtime ladder unchanged
/// at a revision date MUST restate it here. Overtime is validated identically to the preset's overtime
/// (basis day|week, rateMode multiplier|fixedPerHour, at most 3 strictly-ascending tiers).
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class RegionSetupSchedulingRuleRateRevision
{
    public string? ValidFrom { get; set; }

    public decimal? NightRate { get; set; }

    public decimal? HolidayRate { get; set; }

    public decimal? We1Rate { get; set; }

    public decimal? We2Rate { get; set; }

    public decimal? We3Rate { get; set; }

    public RegionSetupOvertime? Overtime { get; set; }
}
