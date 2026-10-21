// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Validates the surcharges section of a region-setup profile (rates, night window, per-type rate modes
/// and hourly minimums, stacking mode, overtime basis/rate mode/tiers) and appends the resulting global
/// settings. Also used by the best-effort backfills for fields added to the section later.
/// </summary>
/// <param name="surcharges">Surcharges section of the profile; null adds nothing</param>
/// <param name="settings">Planned (type, value) setting writes the section is appended to</param>

using System.Globalization;
using Klacks.Api.Application.DTOs.Setup;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;

namespace Klacks.Api.Infrastructure.Services.Settings.RegionSetup;

internal static class RegionSetupSurchargeSettingsBuilder
{
    internal const int MaxOvertimeTiers = 3;

    private const string SurchargeTypeNight = "night";
    private const string SurchargeTypeHoliday = "holiday";
    private const string SurchargeTypeWeekend1 = "weekend1";
    private const string SurchargeTypeWeekend2 = "weekend2";
    private const string SurchargeTypeWeekend3 = "weekend3";

    private const string RateModeMultiplier = CompanyRuleParameterValues.RateModeMultiplier;
    private const string RateModeFixedPerHour = CompanyRuleParameterValues.RateModeFixedPerHour;
    private const string RateModeFixedPerShift = CompanyRuleParameterValues.RateModeFixedPerShift;

    internal static readonly IReadOnlyList<string> OvertimeTierRateKeys = new[]
    {
        SettingKeys.OvertimeTier1Rate, SettingKeys.OvertimeTier2Rate, SettingKeys.OvertimeTier3Rate,
    };

    private static readonly IReadOnlyList<string> OvertimeTierAfterHoursKeys = new[]
    {
        SettingKeys.OvertimeTier1AfterHours, SettingKeys.OvertimeTier2AfterHours, SettingKeys.OvertimeTier3AfterHours,
    };

    private static readonly IReadOnlyDictionary<string, string> RateModeSettingKeysByType = new Dictionary<string, string>
    {
        [SurchargeTypeNight] = SettingKeys.SurchargeNightRateMode,
        [SurchargeTypeHoliday] = SettingKeys.SurchargeHolidayRateMode,
        [SurchargeTypeWeekend1] = SettingKeys.SurchargeWE1RateMode,
        [SurchargeTypeWeekend2] = SettingKeys.SurchargeWE2RateMode,
        [SurchargeTypeWeekend3] = SettingKeys.SurchargeWE3RateMode,
    };

    private static readonly IReadOnlyDictionary<string, string> MinimumPerHourSettingKeysByType = new Dictionary<string, string>
    {
        [SurchargeTypeNight] = SettingKeys.SurchargeNightMinimumPerHour,
        [SurchargeTypeHoliday] = SettingKeys.SurchargeHolidayMinimumPerHour,
        [SurchargeTypeWeekend1] = SettingKeys.SurchargeWE1MinimumPerHour,
        [SurchargeTypeWeekend2] = SettingKeys.SurchargeWE2MinimumPerHour,
        [SurchargeTypeWeekend3] = SettingKeys.SurchargeWE3MinimumPerHour,
    };

    internal static void AddSurchargeSettings(RegionSetupSurcharges? surcharges, List<(string Type, string Value)> settings)
    {
        if (surcharges == null)
        {
            return;
        }

        RegionSetupSettingAppender.AddNumber(settings, SettingKeys.NightRate, surcharges.NightRate);
        RegionSetupSettingAppender.AddNumber(settings, SettingKeys.HolidayRate, surcharges.HolidayRate);
        RegionSetupSettingAppender.AddNumber(settings, SettingKeys.WE1Rate, surcharges.We1Rate);
        RegionSetupSettingAppender.AddNumber(settings, SettingKeys.WE2Rate, surcharges.We2Rate);
        RegionSetupSettingAppender.AddNumber(settings, SettingKeys.WE3Rate, surcharges.We3Rate);
        AddNightWindowSettings(surcharges.NightWindow, settings);
        AddSurchargeRateModeSettings(surcharges.RateModes, settings);
        AddSurchargeMinimumsPerHourSettings(surcharges.MinimumsPerHour, settings);
        AddStackingModeSetting(surcharges.StackingMode, settings);
        AddOvertimeSettings(surcharges.Overtime, settings);
    }

    internal static void AddOvertimeTierSettings(List<RegionSetupOvertimeTier>? tiers, List<(string Type, string Value)> settings)
    {
        if (tiers == null)
        {
            return;
        }

        if (tiers.Count > MaxOvertimeTiers)
        {
            throw new InvalidRequestException(
                $"Region setup: surcharges.overtime.tiers supports at most {MaxOvertimeTiers} entries (Overtime1/2/3), found {tiers.Count}.");
        }

        var previousAfterHours = decimal.MinValue;
        for (var i = 0; i < tiers.Count; i++)
        {
            var tier = tiers[i];
            if (tier.AfterHours == null || tier.Rate == null)
            {
                throw new InvalidRequestException($"Region setup: surcharges.overtime.tiers[{i}] requires 'afterHours' and 'rate'.");
            }

            if (tier.AfterHours <= previousAfterHours)
            {
                throw new InvalidRequestException($"Region setup: surcharges.overtime.tiers[{i}].afterHours must be strictly ascending across tiers.");
            }

            if (tier.Rate <= 0)
            {
                throw new InvalidRequestException($"Region setup: surcharges.overtime.tiers[{i}].rate must be greater than zero.");
            }

            previousAfterHours = tier.AfterHours.Value;
            settings.Add((OvertimeTierAfterHoursKeys[i], tier.AfterHours.Value.ToString(CultureInfo.InvariantCulture)));
            settings.Add((OvertimeTierRateKeys[i], tier.Rate.Value.ToString(CultureInfo.InvariantCulture)));
        }
    }

    private static void AddStackingModeSetting(string? stackingMode, List<(string Type, string Value)> settings)
    {
        if (string.IsNullOrWhiteSpace(stackingMode))
        {
            return;
        }

        settings.Add((SettingKeys.SurchargeStackingMode, RegionSetupFieldParser.ValidateStackingMode(stackingMode, "surcharges.stackingMode")));
    }

    private static void AddOvertimeSettings(RegionSetupOvertime? overtime, List<(string Type, string Value)> settings)
    {
        if (overtime == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(overtime.Basis))
        {
            settings.Add((SettingKeys.OvertimeBasis, RegionSetupFieldParser.ValidateOvertimeBasis(overtime.Basis, "surcharges.overtime.basis")));
        }

        if (!string.IsNullOrWhiteSpace(overtime.RateMode))
        {
            settings.Add((SettingKeys.OvertimeRateMode, RegionSetupFieldParser.ValidateOvertimeRateMode(overtime.RateMode, "surcharges.overtime.rateMode")));
        }

        AddOvertimeTierSettings(overtime.Tiers, settings);
    }

    private static void AddSurchargeRateModeSettings(Dictionary<string, string>? rateModes, List<(string Type, string Value)> settings)
    {
        if (rateModes == null)
        {
            return;
        }

        foreach (var (typeName, mode) in rateModes)
        {
            var settingKey = ResolveSurchargeTypeSettingKey(typeName, RateModeSettingKeysByType, "surcharges.rateModes");
            var normalizedMode = ValidateRateMode(mode, $"surcharges.rateModes.{typeName}");
            settings.Add((settingKey, normalizedMode));
        }
    }

    private static void AddSurchargeMinimumsPerHourSettings(Dictionary<string, decimal>? minimumsPerHour, List<(string Type, string Value)> settings)
    {
        if (minimumsPerHour == null)
        {
            return;
        }

        foreach (var (typeName, value) in minimumsPerHour)
        {
            var settingKey = ResolveSurchargeTypeSettingKey(typeName, MinimumPerHourSettingKeysByType, "surcharges.minimumsPerHour");
            settings.Add((settingKey, value.ToString(CultureInfo.InvariantCulture)));
        }
    }

    private static string ResolveSurchargeTypeSettingKey(string typeName, IReadOnlyDictionary<string, string> settingKeysByType, string fieldName)
    {
        var normalized = typeName.Trim().ToLowerInvariant();
        if (!settingKeysByType.TryGetValue(normalized, out var settingKey))
        {
            throw new InvalidRequestException(
                $"Region setup: unknown surcharge type '{typeName}' in {fieldName}. Valid values: night, holiday, weekend1, weekend2, weekend3.");
        }

        return settingKey;
    }

    private static string ValidateRateMode(string value, string fieldName)
    {
        var normalized = value.Trim().ToLowerInvariant();
        if (normalized != RateModeMultiplier && normalized != RateModeFixedPerHour && normalized != RateModeFixedPerShift)
        {
            throw new InvalidRequestException($"Region setup: '{value}' in {fieldName} must be 'multiplier', 'fixedPerHour' or 'fixedPerShift'.");
        }

        return normalized;
    }

    private static void AddNightWindowSettings(RegionSetupNightWindow? nightWindow, List<(string Type, string Value)> settings)
    {
        if (nightWindow == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(nightWindow.Start))
        {
            RegionSetupFieldParser.ValidateTimeOfDay(nightWindow.Start, "surcharges.nightWindow.start");
            settings.Add((SettingKeys.SurchargeNightStart, nightWindow.Start.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(nightWindow.End))
        {
            RegionSetupFieldParser.ValidateTimeOfDay(nightWindow.End, "surcharges.nightWindow.end");
            settings.Add((SettingKeys.SurchargeNightEnd, nightWindow.End.Trim()));
        }
    }
}
