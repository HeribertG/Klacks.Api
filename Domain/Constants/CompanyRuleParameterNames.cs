// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Stable parameter names used by the company-rule intake catalog, validator and drafts. The surcharge
/// names map 1:1 to the concrete <see cref="SettingKeys"/> entries the apply step writes; the
/// counter-rule and macro names map to <see cref="Klacks.Api.Domain.Models.Scheduling.CounterRule"/>
/// and <see cref="Klacks.Api.Domain.Models.Settings.Macro"/> properties respectively.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class CompanyRuleParameterNames
{
    public const string NightStart = "nightStart";
    public const string NightEnd = "nightEnd";

    public const string NightRate = "nightRate";
    public const string HolidayRate = "holidayRate";
    public const string Weekend1Rate = "weekend1Rate";
    public const string Weekend2Rate = "weekend2Rate";
    public const string Weekend3Rate = "weekend3Rate";

    public const string NightRateMode = "nightRateMode";
    public const string HolidayRateMode = "holidayRateMode";
    public const string Weekend1RateMode = "weekend1RateMode";
    public const string Weekend2RateMode = "weekend2RateMode";
    public const string Weekend3RateMode = "weekend3RateMode";

    public const string NightMinimumPerHour = "nightMinimumPerHour";
    public const string HolidayMinimumPerHour = "holidayMinimumPerHour";
    public const string Weekend1MinimumPerHour = "weekend1MinimumPerHour";
    public const string Weekend2MinimumPerHour = "weekend2MinimumPerHour";
    public const string Weekend3MinimumPerHour = "weekend3MinimumPerHour";

    public const string StackingMode = "stackingMode";

    public const string OvertimeBasis = "overtimeBasis";
    public const string OvertimeRateMode = "overtimeRateMode";
    public const string OvertimeTier1AfterHours = "overtimeTier1AfterHours";
    public const string OvertimeTier1Rate = "overtimeTier1Rate";
    public const string OvertimeTier2AfterHours = "overtimeTier2AfterHours";
    public const string OvertimeTier2Rate = "overtimeTier2Rate";
    public const string OvertimeTier3AfterHours = "overtimeTier3AfterHours";
    public const string OvertimeTier3Rate = "overtimeTier3Rate";

    public const string EventType = "eventType";
    public const string Period = "period";
    public const string Threshold = "threshold";
    public const string HoursThreshold = "hoursThreshold";
    public const string Enforcement = "enforcement";
    public const string SchedulingRuleName = "schedulingRuleName";
    public const string CounterRuleName = "name";

    public const string MacroName = "name";
    public const string MacroScript = "script";
    public const string MacroDescription = "description";
    public const string MacroCategory = "category";

    public const string AtLeastOneSurchargeParameter = "atLeastOneSurchargeParameter";
}
