// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Services.Setup;

/// <summary>
/// Value payload of one desired SchedulingRuleRateRevision row for the region-setup entity import (K20).
/// ValidFrom identifies the revision within its parent preset; every field participates in the content
/// hash. Keep this record, ComputeRateRevisionContentHash and CopyRateRevisionValues/ToImportValues in
/// RegionSetupService in sync — a field present in only some of them silently breaks the customer-edit
/// detection.
/// </summary>
public sealed record SchedulingRuleRateRevisionImportValues(
    DateOnly ValidFrom,
    decimal? NightRate,
    decimal? HolidayRate,
    decimal? We1Rate,
    decimal? We2Rate,
    decimal? We3Rate);
