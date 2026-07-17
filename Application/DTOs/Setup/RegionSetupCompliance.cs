// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.Setup;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class RegionSetupCompliance
{
    public RegionSetupQualifications? Qualifications { get; set; }

    public RegionSetupEnforcement? Enforcement { get; set; }

    public RegionSetupRosterPublication? RosterPublication { get; set; }

    /// <summary>
    /// Settings-based section (K12 stage 1): configures compensatory-rest deadline tracking. Not an
    /// entity import — only enabled/deadlineDays/autoPlan configuration. autoPlan is stage 2 and rejected.
    /// </summary>
    public RegionSetupCompensatoryRest? CompensatoryRest { get; set; }

    /// <summary>
    /// Entity-import section (K20): each entry upserts one PeriodCapRule row via the per-row
    /// ImportSourceKey/ImportContentHash mechanism, independent of the compliance section marker.
    /// </summary>
    public List<RegionSetupPeriodCap>? PeriodCaps { get; set; }

    /// <summary>
    /// Entity-import section (K10): each entry upserts one RestDayRotationRule row via the same
    /// per-row import mechanism as PeriodCaps.
    /// </summary>
    public List<RegionSetupRestDayRotation>? RestDayRotations { get; set; }

    /// <summary>
    /// Entity-import section (K18): each entry upserts one CounterRule row via the same per-row
    /// import mechanism as PeriodCaps.
    /// </summary>
    public List<RegionSetupCounterRule>? CounterRules { get; set; }
}
