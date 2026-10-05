// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reason codes of a holiday outcome diagnosis: why a holiday time surcharge or a holiday-work warning applies or
/// not for one person on one date. The first failing step of the decision chain wins.
/// </summary>

namespace Klacks.Api.Application.Constants;

public static class HolidayOutcomeReasonCodes
{
    public const string Applies = "Applies";
    public const string NoHolidayCalendar = "NoHolidayCalendar";
    public const string NotAHoliday = "NotAHoliday";
    public const string ReminderOnlyEntry = "ReminderOnlyEntry";
    public const string RuleNotOfficial = "RuleNotOfficial";
    public const string NotMarkedForTimeSurcharge = "NotMarkedForTimeSurcharge";
    public const string HolidayRateZero = "HolidayRateZero";
    public const string ExemptionApplies = "ExemptionApplies";
    public const string Unexplained = "Unexplained";
}
