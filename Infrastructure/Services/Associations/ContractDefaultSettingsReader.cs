// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Translates the raw settings rows (key -> string value) into <see cref="ContractDefaultSettings"/>.
/// Missing or unparsable numbers fall back to 0, missing weekday/shift-work flags to true and a missing
/// rate mode to Multiplier.
/// </summary>
/// <param name="settings">Settings rows keyed by SettingKeys, restricted to <see cref="Keys"/></param>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Infrastructure.Services.Associations;

internal static class ContractDefaultSettingsReader
{
    private const string FalseLiteral = "false";

    public static readonly string[] Keys =
    [
        SettingKeys.NightRate, SettingKeys.HolidayRate, SettingKeys.WE1Rate, SettingKeys.WE2Rate, SettingKeys.WE3Rate,
        SettingKeys.SurchargeNightStart, SettingKeys.SurchargeNightEnd,
        SettingKeys.SurchargeNightRateMode, SettingKeys.SurchargeHolidayRateMode,
        SettingKeys.SurchargeWE1RateMode, SettingKeys.SurchargeWE2RateMode, SettingKeys.SurchargeWE3RateMode,
        SettingKeys.SurchargeNightMinimumPerHour, SettingKeys.SurchargeHolidayMinimumPerHour,
        SettingKeys.SurchargeWE1MinimumPerHour, SettingKeys.SurchargeWE2MinimumPerHour, SettingKeys.SurchargeWE3MinimumPerHour,
        SettingKeys.GuaranteedHours, SettingKeys.FullTime, SettingKeys.DefaultWorkingHours,
        SettingKeys.OvertimeThreshold, SettingKeys.MaximumHours, SettingKeys.MinimumHours,
        SettingKeys.PaymentInterval, SettingKeys.VacationDaysPerYear,
        SettingKeys.SchedulingMaxWorkDays, SettingKeys.SchedulingMinRestDays,
        SettingKeys.SchedulingMinPauseHours, SettingKeys.SchedulingMaxDailySpanHours, SettingKeys.SchedulingMaxOptimalGap,
        SettingKeys.SchedulingMaxDailyHours, SettingKeys.SchedulingMaxWeeklyHours,
        SettingKeys.SchedulingMaxConsecutiveDays,
        SettingKeys.SchedulingDefaultWorkOnMonday, SettingKeys.SchedulingDefaultWorkOnTuesday,
        SettingKeys.SchedulingDefaultWorkOnWednesday, SettingKeys.SchedulingDefaultWorkOnThursday,
        SettingKeys.SchedulingDefaultWorkOnFriday, SettingKeys.SchedulingDefaultWorkOnSaturday,
        SettingKeys.SchedulingDefaultWorkOnSunday, SettingKeys.SchedulingDefaultPerformsShiftWork
    ];

    public static ContractDefaultSettings FromSettings(IReadOnlyDictionary<string, string> settings)
    {
        string? Value(string key) => settings.GetValueOrDefault(key);

        return new ContractDefaultSettings
        {
            NightRate = ParseDecimal(Value(SettingKeys.NightRate)),
            HolidayRate = ParseDecimal(Value(SettingKeys.HolidayRate)),
            WE1Rate = ParseDecimal(Value(SettingKeys.WE1Rate)),
            WE2Rate = ParseDecimal(Value(SettingKeys.WE2Rate)),
            WE3Rate = ParseDecimal(Value(SettingKeys.WE3Rate)),
            NightRateMode = ParseRateMode(Value(SettingKeys.SurchargeNightRateMode)),
            HolidayRateMode = ParseRateMode(Value(SettingKeys.SurchargeHolidayRateMode)),
            WE1RateMode = ParseRateMode(Value(SettingKeys.SurchargeWE1RateMode)),
            WE2RateMode = ParseRateMode(Value(SettingKeys.SurchargeWE2RateMode)),
            WE3RateMode = ParseRateMode(Value(SettingKeys.SurchargeWE3RateMode)),
            NightMinimumPerHour = ParseNullableDecimal(Value(SettingKeys.SurchargeNightMinimumPerHour)),
            HolidayMinimumPerHour = ParseNullableDecimal(Value(SettingKeys.SurchargeHolidayMinimumPerHour)),
            WE1MinimumPerHour = ParseNullableDecimal(Value(SettingKeys.SurchargeWE1MinimumPerHour)),
            WE2MinimumPerHour = ParseNullableDecimal(Value(SettingKeys.SurchargeWE2MinimumPerHour)),
            WE3MinimumPerHour = ParseNullableDecimal(Value(SettingKeys.SurchargeWE3MinimumPerHour)),
            NightStart = ParseTimeOfDay(Value(SettingKeys.SurchargeNightStart), SurchargeDefaults.NightStart),
            NightEnd = ParseTimeOfDay(Value(SettingKeys.SurchargeNightEnd), SurchargeDefaults.NightEnd),
            GuaranteedHours = ParseDecimal(Value(SettingKeys.GuaranteedHours)),
            FullTime = ParseDecimal(Value(SettingKeys.FullTime)),
            DefaultWorkingHours = ParseDecimal(Value(SettingKeys.DefaultWorkingHours)),
            OvertimeThreshold = ParseDecimal(Value(SettingKeys.OvertimeThreshold)),
            MaximumHours = ParseDecimal(Value(SettingKeys.MaximumHours)),
            MinimumHours = ParseDecimal(Value(SettingKeys.MinimumHours)),
            PaymentInterval = ParseInt(Value(SettingKeys.PaymentInterval)),
            VacationDaysPerYear = ParseInt(Value(SettingKeys.VacationDaysPerYear)),
            MaxWorkDays = ParseInt(Value(SettingKeys.SchedulingMaxWorkDays)),
            MinRestDays = ParseDecimal(Value(SettingKeys.SchedulingMinRestDays)),
            MinPauseHours = ParseDecimal(Value(SettingKeys.SchedulingMinPauseHours)),
            MaxDailySpanHours = ParseDecimal(Value(SettingKeys.SchedulingMaxDailySpanHours)),
            MaxOptimalGap = ParseDecimal(Value(SettingKeys.SchedulingMaxOptimalGap)),
            MaxDailyHours = ParseDecimal(Value(SettingKeys.SchedulingMaxDailyHours)),
            MaxWeeklyHours = ParseDecimal(Value(SettingKeys.SchedulingMaxWeeklyHours)),
            MaxConsecutiveDays = ParseInt(Value(SettingKeys.SchedulingMaxConsecutiveDays)),
            WorkOnMonday = ParseBool(Value(SettingKeys.SchedulingDefaultWorkOnMonday)),
            WorkOnTuesday = ParseBool(Value(SettingKeys.SchedulingDefaultWorkOnTuesday)),
            WorkOnWednesday = ParseBool(Value(SettingKeys.SchedulingDefaultWorkOnWednesday)),
            WorkOnThursday = ParseBool(Value(SettingKeys.SchedulingDefaultWorkOnThursday)),
            WorkOnFriday = ParseBool(Value(SettingKeys.SchedulingDefaultWorkOnFriday)),
            WorkOnSaturday = ParseBool(Value(SettingKeys.SchedulingDefaultWorkOnSaturday)),
            WorkOnSunday = ParseBool(Value(SettingKeys.SchedulingDefaultWorkOnSunday)),
            PerformsShiftWork = ParseBool(Value(SettingKeys.SchedulingDefaultPerformsShiftWork))
        };
    }

    private static decimal ParseDecimal(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : 0;
    }

    private static int ParseInt(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        return int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : 0;
    }

    private static string ParseTimeOfDay(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private static decimal? ParseNullableDecimal(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : null;
    }

    private static SurchargeRateMode ParseRateMode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return SurchargeRateMode.Multiplier;

        return Enum.TryParse<SurchargeRateMode>(value, ignoreCase: true, out var mode) ? mode : SurchargeRateMode.Multiplier;
    }

    // Absent rows default to true: the seed ships every SCHEDULING_DEFAULT_* flag as true and a
    // contract-less fallback that cannot work on any day would silently exclude the client from
    // planning (observed live: only early shifts were planned for a whole month).
    private static bool ParseBool(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return true;

        return !string.Equals(value, FalseLiteral, StringComparison.OrdinalIgnoreCase);
    }
}
