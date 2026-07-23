// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Stable parameter identifiers the planning-profile setup dialog collects across turns. BaseIndustry
/// drives which industry template rules are copied (or a blank start); the remaining names map onto
/// <see cref="Klacks.Api.Domain.Models.Scheduling.SchedulingRule"/> fields and act as overrides on the
/// created rules. AtLeastOneOverride is a virtual name reported by the validator when a from-scratch
/// profile carries no field value at all.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class PlanningProfileParameterNames
{
    public const string BaseIndustry = "baseIndustry";
    public const string ProfileName = "profileName";

    public const string DefaultWorkingHours = "defaultWorkingHours";
    public const string FullTimeHours = "fullTimeHours";
    public const string MaxDailyHours = "maxDailyHours";
    public const string MaxWeeklyHours = "maxWeeklyHours";
    public const string MaxConsecutiveDays = "maxConsecutiveDays";
    public const string MinRestDays = "minRestDays";
    public const string MinPauseHours = "minPauseHours";
    public const string OvertimeThreshold = "overtimeThreshold";
    public const string OvertimeBasis = "overtimeBasis";
    public const string NightStart = "nightStart";
    public const string NightEnd = "nightEnd";
    public const string NightRate = "nightRate";
    public const string HolidayRate = "holidayRate";
    public const string VacationDaysPerYear = "vacationDaysPerYear";

    public const string AtLeastOneOverride = "atLeastOneOverride";
}
