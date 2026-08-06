// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Stable rule-type identifiers used both by the region-setup compliance.enforcement.rules map and by
/// the pre-commit checker's per-rule enforcement-mode lookup. Deliberately match SchedulingPolicy's
/// property names so a value accepted by the wizard placement engine and the post-hoc validator is the
/// same value governed here.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ComplianceRuleNames
{
    public const string MaxDailyHours = "maxDailyHours";
    public const string MaxWeeklyHours = "maxWeeklyHours";
    public const string MinRestHours = "minRestHours";
    public const string MinRestDays = "minRestDays";
    public const string MaxConsecutiveDays = "maxConsecutiveDays";
    public const string PeriodCap = "periodCap";
    public const string RollingAverage = "rollingAverage";
    public const string RestDayRotation = "restDayRotation";
    public const string CounterRule = "counterRule";
    public const string CompensatoryRest = "compensatoryRest";
    public const string RestrictedTimeWindow = "restrictedTimeWindow";
    public const string HolidayWork = "holidayWork";

    /// <summary>
    /// CommentParams key tagging a ScheduleValidationNotificationDto whose Type was escalated from
    /// Warning to Error by Block-mode enforcement (as opposed to a structural Error such as a
    /// collision or a missing mandatory qualification). Lets a save-command handler offer the
    /// supervisor-override path only for this kind of block.
    /// </summary>
    public const string EnforcementRuleParamKey = "enforcementRule";
}
