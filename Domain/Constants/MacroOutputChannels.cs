// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// OUTPUT channels the backend actually processes when a macro runs (MacroCompilationService): channel 1 is
/// the macro result, channels 10-14 are the typed night, weekend and holiday surcharges. Output on any other
/// channel is silently discarded, so macros written by the assistant may only use these channels. Surcharges
/// lists the surcharge channels 10-14 alone.
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Constants;

public static class MacroOutputChannels
{
    public static readonly IReadOnlySet<int> Supported = new HashSet<int>
    {
        (int)MacroTypeEnum.DefaultResult,
        (int)MacroTypeEnum.SurchargeNight,
        (int)MacroTypeEnum.SurchargeWeekend1,
        (int)MacroTypeEnum.SurchargeWeekend2,
        (int)MacroTypeEnum.SurchargeWeekend3,
        (int)MacroTypeEnum.SurchargeHoliday
    };

    public static readonly IReadOnlySet<int> Surcharges = new HashSet<int>
    {
        (int)MacroTypeEnum.SurchargeNight,
        (int)MacroTypeEnum.SurchargeWeekend1,
        (int)MacroTypeEnum.SurchargeWeekend2,
        (int)MacroTypeEnum.SurchargeWeekend3,
        (int)MacroTypeEnum.SurchargeHoliday
    };
}
