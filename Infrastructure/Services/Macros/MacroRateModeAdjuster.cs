// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reinterprets a macro's typed surcharge output according to the configured RateMode per surcharge type — the one place
/// shared by the production work path (WorkMacroService) and the macro dry-run. Macros always compute Amount as
/// SegmentHours * Rate (verified against the built-in "AllShift" macro); for Multiplier and FixedPerHour this arithmetic
/// already yields the desired result, so only FixedPerShift needs an override (the flat Rate once, independent of the
/// segment duration), and Multiplier honours an optional minimum per hour. The result value moves by the same delta, so a
/// non-surcharge portion of a custom macro's total is preserved rather than overwritten.
/// </summary>
/// <param name="result">The aggregated macro result of one work or work change</param>
/// <param name="macroData">The inputs of that run, carrying the rates, rate modes and minimums</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Macros;

namespace Klacks.Api.Infrastructure.Services.Macros;

public static class MacroRateModeAdjuster
{
    public static MacroExecutionResult Apply(MacroExecutionResult result, MacroData macroData)
    {
        if (!result.Success || result.Surcharges.Count == 0)
        {
            return result;
        }

        var adjustedItems = result.Surcharges
            .Select(item => item with { Amount = AdjustAmount(item.Type, item.Amount, macroData) })
            .ToList();

        var delta = adjustedItems.Sum(item => item.Amount) - result.Surcharges.Sum(item => item.Amount);
        var adjustedResultValue = result.ResultValue.HasValue
            ? Math.Round(result.ResultValue.Value + delta, MacroAmountPrecision.DecimalPlaces)
            : result.ResultValue;

        return new MacroExecutionResult(result.Success, adjustedResultValue, adjustedItems);
    }

    private static decimal AdjustAmount(SurchargeType type, decimal amount, MacroData macroData)
    {
        var (rate, mode, minimumPerHour) = GetRateConfig(type, macroData);

        if (mode == SurchargeRateMode.FixedPerShift)
        {
            return amount == 0m ? 0m : rate;
        }

        if (mode == SurchargeRateMode.Multiplier && minimumPerHour.HasValue && rate != 0m)
        {
            var segmentHours = amount / rate;
            var minimumAmount = minimumPerHour.Value * segmentHours;
            return Math.Max(amount, minimumAmount);
        }

        return amount;
    }

    private static (decimal Rate, SurchargeRateMode Mode, decimal? MinimumPerHour) GetRateConfig(SurchargeType type, MacroData macroData) => type switch
    {
        SurchargeType.Night => (macroData.NightRate, macroData.NightRateMode, macroData.NightMinimumPerHour),
        SurchargeType.Weekend1 => (macroData.WE1Rate, macroData.WE1RateMode, macroData.WE1MinimumPerHour),
        SurchargeType.Weekend2 => (macroData.WE2Rate, macroData.WE2RateMode, macroData.WE2MinimumPerHour),
        SurchargeType.Weekend3 => (macroData.WE3Rate, macroData.WE3RateMode, macroData.WE3MinimumPerHour),
        SurchargeType.Holiday => (macroData.HolidayRate, macroData.HolidayRateMode, macroData.HolidayMinimumPerHour),
        _ => (0m, SurchargeRateMode.Multiplier, null)
    };
}
