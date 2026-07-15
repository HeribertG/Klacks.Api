// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// Countable per-person events for CounterRule (K18), all derivable from Work rows without extra
/// tracking. NightShift counts works overlapping the night window; WorkedDayInWeek counts distinct
/// worked calendar days; ShiftExceedingHours counts works longer than the rule's HoursThreshold.
/// </summary>
public enum CounterEventType
{
    NightShift = 1,
    WorkedDayInWeek = 2,
    ShiftExceedingHours = 3
}
