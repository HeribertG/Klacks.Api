// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Value payload the region-setup compliance.periodCaps entity-import section writes into a
/// PeriodCapRule row on Insert/Update. Exactly one of the two field groups is meaningful per row: for a
/// fixed-period entry, Period/Scope/CapHours are set and RollingWindowWeeks/MaxAverageWeeklyHours are
/// null; for a K6 rolling-average entry, RollingWindowWeeks/MaxAverageWeeklyHours are set and
/// Period/Scope/CapHours carry unused placeholder defaults.
/// </summary>
/// <param name="Period">Recurring window the cap covers (Month/Quarter/Year); unused in rolling-average mode</param>
/// <param name="Scope">What kind of hours the cap counts; stage 1 supports TotalHours only; unused in rolling-average mode</param>
/// <param name="CapHours">Hour threshold that must not be exceeded within the period; unused in rolling-average mode</param>
/// <param name="WarnAtPercent">Reserved percentage threshold for an approaching-cap warning (not yet evaluated)</param>
/// <param name="RollingWindowWeeks">K6: width in weeks of the trailing window; null for fixed-period entries</param>
/// <param name="MaxAverageWeeklyHours">K6: average weekly hours threshold over RollingWindowWeeks; null for fixed-period entries</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.Setup;

public sealed record PeriodCapRuleImportValues(
    PeriodCapPeriod Period,
    PeriodCapScope Scope,
    decimal CapHours,
    int? WarnAtPercent,
    int? RollingWindowWeeks,
    decimal? MaxAverageWeeklyHours);
