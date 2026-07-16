// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Maps each surcharge company-rule intake parameter name (camelCase, see
/// <see cref="CompanyRuleParameterNames"/>) to the concrete <see cref="SettingKeys"/> entry the apply
/// step reads for the revert snapshot and writes the new value to. The map covers every surcharge
/// parameter the catalog collects, so a surcharge rule can be applied and reverted without a single
/// hard-coded setting key at the call site.
/// </summary>

using System.Collections.Generic;

namespace Klacks.Api.Domain.Constants;

public static class CompanyRuleSurchargeSettingMap
{
    public static readonly IReadOnlyDictionary<string, string> ParameterToSettingKey = new Dictionary<string, string>
    {
        [CompanyRuleParameterNames.NightStart] = SettingKeys.SurchargeNightStart,
        [CompanyRuleParameterNames.NightEnd] = SettingKeys.SurchargeNightEnd,

        [CompanyRuleParameterNames.NightRate] = SettingKeys.NightRate,
        [CompanyRuleParameterNames.HolidayRate] = SettingKeys.HolidayRate,
        [CompanyRuleParameterNames.Weekend1Rate] = SettingKeys.WE1Rate,
        [CompanyRuleParameterNames.Weekend2Rate] = SettingKeys.WE2Rate,
        [CompanyRuleParameterNames.Weekend3Rate] = SettingKeys.WE3Rate,

        [CompanyRuleParameterNames.NightRateMode] = SettingKeys.SurchargeNightRateMode,
        [CompanyRuleParameterNames.HolidayRateMode] = SettingKeys.SurchargeHolidayRateMode,
        [CompanyRuleParameterNames.Weekend1RateMode] = SettingKeys.SurchargeWE1RateMode,
        [CompanyRuleParameterNames.Weekend2RateMode] = SettingKeys.SurchargeWE2RateMode,
        [CompanyRuleParameterNames.Weekend3RateMode] = SettingKeys.SurchargeWE3RateMode,

        [CompanyRuleParameterNames.NightMinimumPerHour] = SettingKeys.SurchargeNightMinimumPerHour,
        [CompanyRuleParameterNames.HolidayMinimumPerHour] = SettingKeys.SurchargeHolidayMinimumPerHour,
        [CompanyRuleParameterNames.Weekend1MinimumPerHour] = SettingKeys.SurchargeWE1MinimumPerHour,
        [CompanyRuleParameterNames.Weekend2MinimumPerHour] = SettingKeys.SurchargeWE2MinimumPerHour,
        [CompanyRuleParameterNames.Weekend3MinimumPerHour] = SettingKeys.SurchargeWE3MinimumPerHour,

        [CompanyRuleParameterNames.StackingMode] = SettingKeys.SurchargeStackingMode,

        [CompanyRuleParameterNames.OvertimeBasis] = SettingKeys.OvertimeBasis,
        [CompanyRuleParameterNames.OvertimeRateMode] = SettingKeys.OvertimeRateMode,
        [CompanyRuleParameterNames.OvertimeTier1AfterHours] = SettingKeys.OvertimeTier1AfterHours,
        [CompanyRuleParameterNames.OvertimeTier1Rate] = SettingKeys.OvertimeTier1Rate,
        [CompanyRuleParameterNames.OvertimeTier2AfterHours] = SettingKeys.OvertimeTier2AfterHours,
        [CompanyRuleParameterNames.OvertimeTier2Rate] = SettingKeys.OvertimeTier2Rate,
        [CompanyRuleParameterNames.OvertimeTier3AfterHours] = SettingKeys.OvertimeTier3AfterHours,
        [CompanyRuleParameterNames.OvertimeTier3Rate] = SettingKeys.OvertimeTier3Rate
    };
}
