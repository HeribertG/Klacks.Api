// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.Setup;

/// <summary>
/// One industry block of the top-level industryProfiles map (K20). The map key is the industry slug
/// (e.g. "spitex", "security") and doubles as the QualificationCategory hint for the catalog entries.
/// All payloads are entity imports (never gated by a section marker): SchedulingRulePresets become
/// named SchedulingRule rows selectable on contracts, QualificationCatalog becomes Qualification rows,
/// and PeriodCaps/RestDayRotations become industry-SCOPED rule rows bound to the block's preset — they
/// apply only to clients whose active contract references that rule. A block that carries caps or
/// rotations must therefore contain exactly ONE scheduling rule preset (the binding target).
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class RegionSetupIndustryProfile
{
    public List<RegionSetupSchedulingRulePreset>? SchedulingRulePresets { get; set; }

    public List<RegionSetupQualificationCatalogEntry>? QualificationCatalog { get; set; }

    public List<RegionSetupPeriodCap>? PeriodCaps { get; set; }

    public List<RegionSetupRestDayRotation>? RestDayRotations { get; set; }
}
