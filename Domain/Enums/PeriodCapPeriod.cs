// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The recurring window a <see cref="Klacks.Api.Domain.Models.Scheduling.PeriodCapRule"/> caps hours
/// over. CustomWeeks is a trailing window of <c>CustomPeriodWeeks</c> weeks ending on the evaluated day;
/// it is reserved for a future release (see PeriodCapRule/PeriodCapEvaluator) and not yet importable via
/// region-setup.json.
/// </summary>

namespace Klacks.Api.Domain.Enums;

public enum PeriodCapPeriod
{
    Month = 0,
    Quarter = 1,
    Year = 2,
    CustomWeeks = 3,
}
