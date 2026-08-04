// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The scheduling settings the readiness calculation depends on.
/// </summary>
/// <param name="MaxWorkDays">Cap behind the desired readiness band</param>
/// <param name="MaxConsecutiveDays">Cap behind the maximum readiness band</param>
/// <param name="DefaultPattern">Weekday pattern applied when no usable contract pattern exists</param>

namespace Klacks.Api.Application.Services.Schedules;

public readonly record struct ResourceMonitorSettings(
    int MaxWorkDays,
    int MaxConsecutiveDays,
    WeekdayPattern DefaultPattern);
