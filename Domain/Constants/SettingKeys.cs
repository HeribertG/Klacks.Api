// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Constants;

public static class SettingKeys
{
    public const string GlobalCalendarCountry = "globalCalendarCountry";
    public const string GlobalCalendarState = "globalCalendarState";
    public const string GlobalCalendarSelectionId = "globalCalendarSelectionId";

    public const string NightRate = "nightRate";
    public const string HolidayRate = "holidayRate";
    public const string SaRate = "saRate";
    public const string SoRate = "soRate";
    public const string GuaranteedHours = "guaranteedHours";
    public const string FullTime = "fullTime";

    public const string DefaultWorkingHours = "defaultWorkingHours";
    public const string OvertimeThreshold = "overtimeThreshold";
    public const string MaximumHours = "maximumHours";
    public const string MinimumHours = "minimumHours";
    public const string PaymentInterval = "paymentInterval";
    public const string VacationDaysPerYear = "vacationDaysPerYear";

    public const string SchedulingMaxWorkDays = "SCHEDULING_MAX_WORK_DAYS";
    public const string SchedulingMinRestDays = "SCHEDULING_MIN_REST_DAYS";
    public const string SchedulingMinPauseHours = "SCHEDULING_MIN_PAUSE_HOURS";
    public const string SchedulingMaxOptimalGap = "SCHEDULING_MAX_OPTIMAL_GAP";
    public const string SchedulingMaxDailyHours = "SCHEDULING_MAX_DAILY_HOURS";
    public const string SchedulingMaxWeeklyHours = "SCHEDULING_MAX_WEEKLY_HOURS";
    public const string SchedulingMaxConsecutiveDays = "SCHEDULING_MAX_CONSECUTIVE_DAYS";
}
