// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Associations;

public sealed record EffectiveContractData
{
    public decimal GuaranteedHours { get; init; }

    /// <summary>
    /// Workload share in percent used by absence macros. Contracts whose guaranteed hours derive from
    /// the company-wide value carry their contract percent; explicitly valued contracts derive it as
    /// guaranteed hours / full time (so a part-timer's absence day never credits more than a worked
    /// day); clients without a contract count as full workload.
    /// </summary>
    public decimal WorkloadPercent { get; init; } = MonthlyTargetHoursConstants.FullWorkloadPercent;

    public decimal MaximumHours { get; init; }
    public decimal MinimumHours { get; init; }
    public decimal FullTime { get; init; }
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
    public int PaymentInterval { get; init; }
    public Guid? CalendarSelectionId { get; init; }

    public decimal DefaultWorkingHours { get; init; }
    public decimal OvertimeThreshold { get; init; }
    public int MaxWorkDays { get; init; }
    public decimal MinRestDays { get; init; }
    public decimal MinPauseHours { get; init; }
    public decimal MaxOptimalGap { get; init; }
    public decimal MaxDailyHours { get; init; }
    public decimal MaxWeeklyHours { get; init; }
    public int MaxConsecutiveDays { get; init; }
    public int VacationDaysPerYear { get; init; }

    public bool HasActiveContract { get; init; }
    public Guid? ContractId { get; init; }
    public Guid? SchedulingRuleId { get; init; }

    public bool WorkOnMonday { get; init; } = true;
    public bool WorkOnTuesday { get; init; } = true;
    public bool WorkOnWednesday { get; init; } = true;
    public bool WorkOnThursday { get; init; } = true;
    public bool WorkOnFriday { get; init; } = true;
    public bool WorkOnSaturday { get; init; }
    public bool WorkOnSunday { get; init; }
    public bool PerformsShiftWork { get; init; }
}
