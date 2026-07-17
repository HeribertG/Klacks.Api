// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// The single authoritative set of global setting keys whose stored value feeds the persisted
/// surcharge or overtime calculation, so a committed change to one of them must trigger a thorough
/// recalculation (SurchargeSettingsChangedEvent). Membership is derived from the actual readers:
/// ClientContractDataProvider.LoadDefaultSettingsAsync supplies the rates, rate modes, per-hour
/// minimums, night window, guaranteed/full-time/default working hours and the overtime threshold as
/// defaults into EffectiveContractData, which MacroDataProvider hands to every surcharge macro run;
/// OvertimeConfigResolver reads the OVERTIME_* basis, rate mode and tier ladder (with
/// overtimeThreshold as the tier-1 fallback); WeekConfiguration's weekend days and week start feed
/// MacroDataProvider's weekend classification and OvertimeSurchargeCalculator's week basis window.
/// SURCHARGE_STACKING_MODE is deliberately absent: DefaultShiftMacroResolver reads it only to pick
/// the default macro at shift creation, so changing it never alters the result of recomputing an
/// already persisted work.
///
/// The three WORKTIME_ONCALL_* factor keys are included even though on-call feeds worked HOURS,
/// not surcharges: OnCallConfigResolver turns them into the weighting PeriodHoursService applies to
/// on-call work changes, so a factor change retroactively alters every persisted period's Hours and
/// must trigger the same thorough recomputation that refreshes the ClientPeriodHours cache.
/// WORKTIME_ONCALL_INCLUDE_IN_PERIOD_CAPS is deliberately absent: it only steers live PeriodCapEvaluator
/// subtraction and never changes the cached Hours value.
/// </summary>
public static class SurchargeRelevantSettingKeys
{
    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        SettingKeys.NightRate,
        SettingKeys.HolidayRate,
        SettingKeys.WE1Rate,
        SettingKeys.WE2Rate,
        SettingKeys.WE3Rate,
        SettingKeys.SurchargeNightStart,
        SettingKeys.SurchargeNightEnd,
        SettingKeys.SurchargeNightRateMode,
        SettingKeys.SurchargeHolidayRateMode,
        SettingKeys.SurchargeWE1RateMode,
        SettingKeys.SurchargeWE2RateMode,
        SettingKeys.SurchargeWE3RateMode,
        SettingKeys.SurchargeNightMinimumPerHour,
        SettingKeys.SurchargeHolidayMinimumPerHour,
        SettingKeys.SurchargeWE1MinimumPerHour,
        SettingKeys.SurchargeWE2MinimumPerHour,
        SettingKeys.SurchargeWE3MinimumPerHour,
        SettingKeys.GuaranteedHours,
        SettingKeys.FullTime,
        SettingKeys.DefaultWorkingHours,
        SettingKeys.OvertimeThreshold,
        SettingKeys.OvertimeBasis,
        SettingKeys.OvertimeRateMode,
        SettingKeys.OvertimeTier1AfterHours,
        SettingKeys.OvertimeTier1Rate,
        SettingKeys.OvertimeTier2AfterHours,
        SettingKeys.OvertimeTier2Rate,
        SettingKeys.OvertimeTier3AfterHours,
        SettingKeys.OvertimeTier3Rate,
        SettingKeys.WeekendDays,
        SettingKeys.WeekStartDay,
        SettingKeys.WorktimeOnCallEnabled,
        SettingKeys.WorktimeOnCallPresenceCountsPercent,
        SettingKeys.WorktimeOnCallStandbyCountsPercent
    };
}
