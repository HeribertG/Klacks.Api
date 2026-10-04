// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Installation-wide contract defaults read from the settings table; the last fallback level of the
/// contract -> scheduling rule -> settings resolution in ClientContractDataProvider.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Infrastructure.Services.Associations;

internal sealed record ContractDefaultSettings
{
    public decimal NightRate { get; init; }
    public decimal HolidayRate { get; init; }
    public decimal WE1Rate { get; init; }
    public decimal WE2Rate { get; init; }
    public decimal WE3Rate { get; init; }
    public SurchargeRateMode NightRateMode { get; init; }
    public SurchargeRateMode HolidayRateMode { get; init; }
    public SurchargeRateMode WE1RateMode { get; init; }
    public SurchargeRateMode WE2RateMode { get; init; }
    public SurchargeRateMode WE3RateMode { get; init; }
    public decimal? NightMinimumPerHour { get; init; }
    public decimal? HolidayMinimumPerHour { get; init; }
    public decimal? WE1MinimumPerHour { get; init; }
    public decimal? WE2MinimumPerHour { get; init; }
    public decimal? WE3MinimumPerHour { get; init; }
    public string NightStart { get; init; } = SurchargeDefaults.NightStart;
    public string NightEnd { get; init; } = SurchargeDefaults.NightEnd;
    public decimal GuaranteedHours { get; init; }
    public decimal FullTime { get; init; }
    public decimal DefaultWorkingHours { get; init; }
    public decimal OvertimeThreshold { get; init; }
    public decimal MaximumHours { get; init; }
    public decimal MinimumHours { get; init; }
    public int PaymentInterval { get; init; }
    public int VacationDaysPerYear { get; init; }
    public int MaxWorkDays { get; init; }
    public decimal MinRestDays { get; init; }
    public decimal MinPauseHours { get; init; }
    public decimal MaxOptimalGap { get; init; }
    public decimal MaxDailyHours { get; init; }
    public decimal MaxWeeklyHours { get; init; }
    public int MaxConsecutiveDays { get; init; }
    public bool WorkOnMonday { get; init; }
    public bool WorkOnTuesday { get; init; }
    public bool WorkOnWednesday { get; init; }
    public bool WorkOnThursday { get; init; }
    public bool WorkOnFriday { get; init; }
    public bool WorkOnSaturday { get; init; }
    public bool WorkOnSunday { get; init; }
    public bool PerformsShiftWork { get; init; }
}
