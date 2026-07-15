// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Translation keys emitted by <see cref="Klacks.Api.Application.Services.Schedules.ScheduleValidationBuilder"/>.
/// Shared between the entry-generation code and any consumer that needs to recognise a specific rule
/// (e.g. the compliance enforcement-mode lookup mapping a violation back to its rule type).
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ScheduleValidationKeys
{
    public const string RestViolation = "schedule.error-list.rest-violation";
    public const string Overtime = "schedule.error-list.overtime";
    public const string ConsecutiveDays = "schedule.error-list.consecutive-days";
    public const string WeeklyOvertime = "schedule.error-list.weekly-overtime";
    public const string MinRestDays = "schedule.error-list.min-rest-days";
    public const string Collision = "schedule.error-list.collision";
    public const string PeriodCap = "schedule.error-list.period-cap";
    public const string RollingAverage = "schedule.error-list.rolling-average";
    public const string RestDayRotation = "schedule.error-list.rest-day-rotation";
}
