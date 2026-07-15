// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Constants;

public static class SettingKeys
{
    public const string GlobalCalendarCountry = "globalCalendarCountry";
    public const string GlobalCalendarState = "globalCalendarState";
    public const string GlobalCalendarSelectionId = "globalCalendarSelectionId";

    public const string NightRate = "nightRate";
    public const string HolidayRate = "holidayRate";
    public const string WE1Rate = "saRate";
    public const string WE2Rate = "soRate";
    public const string WE3Rate = "we3Rate";
    public const string SurchargeNightStart = "SURCHARGE_NIGHT_START";
    public const string SurchargeNightEnd = "SURCHARGE_NIGHT_END";

    public const string SurchargeNightRateMode = "SURCHARGE_NIGHT_RATE_MODE";
    public const string SurchargeHolidayRateMode = "SURCHARGE_HOLIDAY_RATE_MODE";
    public const string SurchargeWE1RateMode = "SURCHARGE_WE1_RATE_MODE";
    public const string SurchargeWE2RateMode = "SURCHARGE_WE2_RATE_MODE";
    public const string SurchargeWE3RateMode = "SURCHARGE_WE3_RATE_MODE";

    public const string SurchargeNightMinimumPerHour = "SURCHARGE_NIGHT_MINIMUM_PER_HOUR";
    public const string SurchargeHolidayMinimumPerHour = "SURCHARGE_HOLIDAY_MINIMUM_PER_HOUR";
    public const string SurchargeWE1MinimumPerHour = "SURCHARGE_WE1_MINIMUM_PER_HOUR";
    public const string SurchargeWE2MinimumPerHour = "SURCHARGE_WE2_MINIMUM_PER_HOUR";
    public const string SurchargeWE3MinimumPerHour = "SURCHARGE_WE3_MINIMUM_PER_HOUR";
    public const string GuaranteedHours = "guaranteedHours";
    public const string FullTime = "fullTime";

    public const string DefaultWorkingHours = "defaultWorkingHours";
    public const string OvertimeThreshold = "overtimeThreshold";
    public const string MaximumHours = "maximumHours";
    public const string MinimumHours = "minimumHours";
    public const string PaymentInterval = "paymentInterval";
    public const string VacationDaysPerYear = "vacationDaysPerYear";

    public const string ProbationPeriod = "probationPeriod";
    public const string NoticePeriod = "noticePeriod";
    public const string DayVisibleBefore = "dayVisibleBefore";
    public const string DayVisibleAfter = "dayVisibleAfter";

    public const string SchedulingMaxWorkDays = "SCHEDULING_MAX_WORK_DAYS";
    public const string SchedulingMinRestDays = "SCHEDULING_MIN_REST_DAYS";
    public const string SchedulingMinPauseHours = "SCHEDULING_MIN_PAUSE_HOURS";
    public const string SchedulingMaxOptimalGap = "SCHEDULING_MAX_OPTIMAL_GAP";
    public const string SchedulingMaxDailyHours = "SCHEDULING_MAX_DAILY_HOURS";
    public const string SchedulingMaxWeeklyHours = "SCHEDULING_MAX_WEEKLY_HOURS";
    public const string SchedulingMaxConsecutiveDays = "SCHEDULING_MAX_CONSECUTIVE_DAYS";

    public const string SchedulingDefaultWorkOnMonday = "SCHEDULING_DEFAULT_WORK_ON_MONDAY";
    public const string SchedulingDefaultWorkOnTuesday = "SCHEDULING_DEFAULT_WORK_ON_TUESDAY";
    public const string SchedulingDefaultWorkOnWednesday = "SCHEDULING_DEFAULT_WORK_ON_WEDNESDAY";
    public const string SchedulingDefaultWorkOnThursday = "SCHEDULING_DEFAULT_WORK_ON_THURSDAY";
    public const string SchedulingDefaultWorkOnFriday = "SCHEDULING_DEFAULT_WORK_ON_FRIDAY";
    public const string SchedulingDefaultWorkOnSaturday = "SCHEDULING_DEFAULT_WORK_ON_SATURDAY";
    public const string SchedulingDefaultWorkOnSunday = "SCHEDULING_DEFAULT_WORK_ON_SUNDAY";
    public const string SchedulingDefaultPerformsShiftWork = "SCHEDULING_DEFAULT_PERFORMS_SHIFT_WORK";

    public const string WeekendDays = "CALENDAR_WEEKEND_DAYS";
    public const string WeekStartDay = "CALENDAR_WEEK_START_DAY";

    public const string EnabledExportFormats = "ENABLED_EXPORT_FORMATS";
    public const string DefaultPayrollTargetSystem = "DEFAULT_PAYROLL_TARGET_SYSTEM";
    public const string DefaultLanguage = "DEFAULT_LANGUAGE";

    public const string RegionSetupApplied = "REGION_SETUP_APPLIED";
    public const string RegionSetupAppliedLanguages = "REGION_SETUP_APPLIED_LANGUAGES";
    public const string RegionSetupAppliedLocale = "REGION_SETUP_APPLIED_LOCALE";
    public const string RegionSetupAppliedCalendar = "REGION_SETUP_APPLIED_CALENDAR";
    public const string RegionSetupAppliedWorktime = "REGION_SETUP_APPLIED_WORKTIME";
    public const string RegionSetupAppliedSurcharges = "REGION_SETUP_APPLIED_SURCHARGES";
    public const string RegionSetupAppliedExport = "REGION_SETUP_APPLIED_EXPORT";
    public const string RegionSetupAppliedCompliance = "REGION_SETUP_APPLIED_COMPLIANCE";

    public const string QualificationExpiredMandatoryBlocks = "QUALIFICATION_EXPIRED_MANDATORY_BLOCKS";
    public const string QualificationExpiryWarningDays = "QUALIFICATION_EXPIRY_WARNING_DAYS";

    public const string ComplianceEnforcementDefaultMode = "COMPLIANCE_ENFORCEMENT_DEFAULT_MODE";
    public const string ComplianceEnforcementMaxDailyHours = "COMPLIANCE_ENFORCEMENT_MAX_DAILY_HOURS";
    public const string ComplianceEnforcementMaxWeeklyHours = "COMPLIANCE_ENFORCEMENT_MAX_WEEKLY_HOURS";
    public const string ComplianceEnforcementMinRestHours = "COMPLIANCE_ENFORCEMENT_MIN_REST_HOURS";
    public const string ComplianceEnforcementMinRestDays = "COMPLIANCE_ENFORCEMENT_MIN_REST_DAYS";
    public const string ComplianceEnforcementMaxConsecutiveDays = "COMPLIANCE_ENFORCEMENT_MAX_CONSECUTIVE_DAYS";
    public const string ComplianceEnforcementPeriodCap = "COMPLIANCE_ENFORCEMENT_PERIOD_CAP";
    public const string ComplianceEnforcementRollingAverage = "COMPLIANCE_ENFORCEMENT_ROLLING_AVERAGE";
    public const string ComplianceEnforcementRestDayRotation = "COMPLIANCE_ENFORCEMENT_REST_DAY_ROTATION";
    public const string ComplianceEnforcementCounterRule = "COMPLIANCE_ENFORCEMENT_COUNTER_RULE";
    public const string ComplianceEnforcementAllowSupervisorOverride = "COMPLIANCE_ENFORCEMENT_ALLOW_SUPERVISOR_OVERRIDE";

    public const string ComplianceRosterPublicationMinLeadDays = "COMPLIANCE_ROSTER_PUBLICATION_MIN_LEAD_DAYS";
    public const string ComplianceRosterPublicationCountWorkdaysOnly = "COMPLIANCE_ROSTER_PUBLICATION_COUNT_WORKDAYS_ONLY";

    public const string SurchargeStackingMode = "SURCHARGE_STACKING_MODE";
    public const string OvertimeBasis = "OVERTIME_BASIS";
    public const string OvertimeRateMode = "OVERTIME_RATE_MODE";
    public const string OvertimeTier1AfterHours = "OVERTIME_TIER1_AFTER_HOURS";
    public const string OvertimeTier1Rate = "OVERTIME_TIER1_RATE";
    public const string OvertimeTier2AfterHours = "OVERTIME_TIER2_AFTER_HOURS";
    public const string OvertimeTier2Rate = "OVERTIME_TIER2_RATE";
    public const string OvertimeTier3AfterHours = "OVERTIME_TIER3_AFTER_HOURS";
    public const string OvertimeTier3Rate = "OVERTIME_TIER3_RATE";
}
