// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Validates the overtime block of a scheduling rule preset or rate revision (basis, rate mode - flat
/// per-shift rejected - and at most three strictly ascending tiers) into PresetOvertimeValues.
/// </summary>
/// <param name="overtime">Overtime block of the profile entry; null yields all-null values</param>
/// <param name="fieldPrefix">Dotted profile path of the owning entry, used in error messages</param>

using Klacks.Api.Application.DTOs.Setup;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;

namespace Klacks.Api.Infrastructure.Services.Settings.RegionSetup;

internal static class PresetOvertimeParser
{
    private const string OvertimeBasisWeek = CompanyRuleParameterValues.OvertimeBasisWeek;
    private const string RateModeFixedPerHour = CompanyRuleParameterValues.RateModeFixedPerHour;

    // A preset that declares overtime must carry at least tier 1 - basis/rateMode without any tier would
    // be dead configuration the calculator never reads (a rule overrides the global settings only when
    // its tier 1 is complete).
    internal static PresetOvertimeValues Parse(RegionSetupOvertime? overtime, string fieldPrefix)
    {
        if (overtime == null)
        {
            return new PresetOvertimeValues(null, null, null, null, null, null, null, null);
        }

        OvertimeBasis? basis = null;
        if (!string.IsNullOrWhiteSpace(overtime.Basis))
        {
            basis = RegionSetupFieldParser.ValidateOvertimeBasis(overtime.Basis, $"{fieldPrefix}.overtime.basis") == OvertimeBasisWeek
                ? OvertimeBasis.Week
                : OvertimeBasis.Day;
        }

        SurchargeRateMode? rateMode = null;
        if (!string.IsNullOrWhiteSpace(overtime.RateMode))
        {
            rateMode = RegionSetupFieldParser.ValidateOvertimeRateMode(overtime.RateMode, $"{fieldPrefix}.overtime.rateMode") == RateModeFixedPerHour
                ? SurchargeRateMode.FixedPerHour
                : SurchargeRateMode.Multiplier;
        }

        var tiers = ValidateOvertimeTiers(overtime.Tiers, $"{fieldPrefix}.overtime.tiers");
        if (tiers.Count == 0)
        {
            throw new InvalidRequestException(
                $"Region setup: {fieldPrefix}.overtime requires at least one tier (afterHours + rate).");
        }

        return new PresetOvertimeValues(
            basis,
            rateMode,
            tiers.Count > 0 ? tiers[0].AfterHours : null,
            tiers.Count > 0 ? tiers[0].Rate : null,
            tiers.Count > 1 ? tiers[1].AfterHours : null,
            tiers.Count > 1 ? tiers[1].Rate : null,
            tiers.Count > 2 ? tiers[2].AfterHours : null,
            tiers.Count > 2 ? tiers[2].Rate : null);
    }

    private static List<(decimal AfterHours, decimal Rate)> ValidateOvertimeTiers(
        List<RegionSetupOvertimeTier>? tiers, string fieldPrefix)
    {
        var result = new List<(decimal AfterHours, decimal Rate)>();
        if (tiers == null)
        {
            return result;
        }

        if (tiers.Count > RegionSetupSurchargeSettingsBuilder.MaxOvertimeTiers)
        {
            throw new InvalidRequestException(
                $"Region setup: {fieldPrefix} supports at most {RegionSetupSurchargeSettingsBuilder.MaxOvertimeTiers} entries (Overtime1/2/3), found {tiers.Count}.");
        }

        var previousAfterHours = decimal.MinValue;
        for (var i = 0; i < tiers.Count; i++)
        {
            var tier = tiers[i];
            if (tier.AfterHours == null || tier.Rate == null)
            {
                throw new InvalidRequestException($"Region setup: {fieldPrefix}[{i}] requires 'afterHours' and 'rate'.");
            }

            if (tier.AfterHours <= previousAfterHours)
            {
                throw new InvalidRequestException($"Region setup: {fieldPrefix}[{i}].afterHours must be strictly ascending across tiers.");
            }

            if (tier.Rate <= 0)
            {
                throw new InvalidRequestException($"Region setup: {fieldPrefix}[{i}].rate must be greater than zero.");
            }

            previousAfterHours = tier.AfterHours.Value;
            result.Add((tier.AfterHours.Value, tier.Rate.Value));
        }

        return result;
    }
}
