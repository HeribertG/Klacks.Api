// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The single place that binds a <see cref="MacroData"/> into a compiled macro script under the IMPORT names
/// production macros use. Shared by the production execution path and the regression check, so both feed a
/// script exactly the same inputs.
/// </summary>
/// <param name="script">An execution clone of the compiled macro (CompiledScript.CloneForExecution)</param>
/// <param name="data">The inputs of one work, break or test sample</param>

using Klacks.Api.Domain.Models.Macros;
using Klacks.Api.Infrastructure.Scripting;

namespace Klacks.Api.Infrastructure.Services.Macros;

public static class MacroDataImportBinder
{
    private const string Hour = "hour";
    private const string FromHour = "fromhour";
    private const string UntilHour = "untilhour";
    private const string Weekday = "weekday";
    private const string Holiday = "holiday";
    private const string HolidayNextDay = "holidaynextday";
    private const string NightRate = "nightrate";
    private const string HolidayRate = "holidayrate";
    private const string We1Rate = "we1rate";
    private const string We2Rate = "we2rate";
    private const string We3Rate = "we3rate";
    private const string NightStart = "nightstart";
    private const string NightEnd = "nightend";
    private const string GuaranteedHours = "guaranteedhours";
    private const string FullTime = "fulltime";
    private const string Percent = "percent";
    private const string WeekendDay1 = "weekendday1";
    private const string WeekendDay2 = "weekendday2";
    private const string WeekendDay3 = "weekendday3";
    private const int TrueFlag = 1;
    private const int FalseFlag = 0;

    public static void Bind(CompiledScript script, MacroData data)
    {
        script.SetExternalValue(Hour, data.Hour);
        script.SetExternalValue(FromHour, data.FromHour);
        script.SetExternalValue(UntilHour, data.UntilHour);
        script.SetExternalValue(Weekday, data.Weekday);
        script.SetExternalValue(Holiday, data.Holiday ? TrueFlag : FalseFlag);
        script.SetExternalValue(HolidayNextDay, data.HolidayNextDay ? TrueFlag : FalseFlag);
        script.SetExternalValue(NightRate, data.NightRate);
        script.SetExternalValue(HolidayRate, data.HolidayRate);
        script.SetExternalValue(We1Rate, data.WE1Rate);
        script.SetExternalValue(We2Rate, data.WE2Rate);
        script.SetExternalValue(We3Rate, data.WE3Rate);
        script.SetExternalValue(NightStart, data.NightStart);
        script.SetExternalValue(NightEnd, data.NightEnd);
        script.SetExternalValue(GuaranteedHours, data.GuaranteedHours);
        script.SetExternalValue(FullTime, data.FullTime);
        script.SetExternalValue(Percent, data.WorkloadPercent);
        script.SetExternalValue(WeekendDay1, data.WeekendDay1);
        script.SetExternalValue(WeekendDay2, data.WeekendDay2);
        script.SetExternalValue(WeekendDay3, data.WeekendDay3);
    }
}
