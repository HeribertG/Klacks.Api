// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The single place that turns the OUTPUT messages of one macro run into a <see cref="MacroExecutionResult"/> the way
/// production totals them: channel 1 is the result (the last value wins), a non-zero value on one of the channels 10-14
/// becomes a typed surcharge item, every other channel and every value that is not a number is ignored. Shared by the
/// production execution path (MacroCompilationService) and the macro dry-run, so both read a run identically.
/// </summary>
/// <param name="messages">The OUTPUT messages of one execution, in emission order</param>

using System.Globalization;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Macros;
using Klacks.Api.Infrastructure.Scripting;

namespace Klacks.Api.Infrastructure.Services.Macros;

public static class MacroResultAggregator
{
    public static MacroExecutionResult Aggregate(IEnumerable<ResultMessage> messages)
    {
        decimal? resultValue = null;
        var surcharges = new List<MacroSurchargeItem>();
        foreach (var message in messages)
        {
            if (!decimal.TryParse(message.Message, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            {
                continue;
            }

            if (message.Type == (int)MacroTypeEnum.DefaultResult)
            {
                resultValue = parsed;
            }
            else if (parsed != 0m && TryMapSurchargeType(message.Type, out var surchargeType))
            {
                surcharges.Add(new MacroSurchargeItem(surchargeType, parsed));
            }
        }

        return new MacroExecutionResult(true, resultValue, surcharges);
    }

    private static bool TryMapSurchargeType(int messageType, out SurchargeType surchargeType)
    {
        switch ((MacroTypeEnum)messageType)
        {
            case MacroTypeEnum.SurchargeNight:
                surchargeType = SurchargeType.Night;
                return true;
            case MacroTypeEnum.SurchargeWeekend1:
                surchargeType = SurchargeType.Weekend1;
                return true;
            case MacroTypeEnum.SurchargeWeekend2:
                surchargeType = SurchargeType.Weekend2;
                return true;
            case MacroTypeEnum.SurchargeWeekend3:
                surchargeType = SurchargeType.Weekend3;
                return true;
            case MacroTypeEnum.SurchargeHoliday:
                surchargeType = SurchargeType.Holiday;
                return true;
            default:
                surchargeType = default;
                return false;
        }
    }
}
