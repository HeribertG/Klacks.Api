// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.Setup;

/// <summary>
/// One industry block of the top-level industryProfiles map (K20). The map key is the industry slug
/// (e.g. "spitex", "security") and doubles as the QualificationCategory hint for the catalog entries.
/// Both payloads are entity imports (never gated by a section marker): SchedulingRulePresets become
/// named SchedulingRule rows selectable on contracts, QualificationCatalog becomes Qualification rows.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class RegionSetupIndustryProfile
{
    public List<RegionSetupSchedulingRulePreset>? SchedulingRulePresets { get; set; }

    public List<RegionSetupQualificationCatalogEntry>? QualificationCatalog { get; set; }
}
