// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Associations;

public sealed record EffectiveContractData
{
    public decimal GuaranteedHours { get; init; }
    public decimal MaximumHours { get; init; }
    public decimal MinimumHours { get; init; }
    public decimal FullTime { get; init; }
    public decimal NightRate { get; init; }
    public decimal HolidayRate { get; init; }
    public decimal SaRate { get; init; }
    public decimal SoRate { get; init; }
    public int PaymentInterval { get; init; }
    public Guid? CalendarSelectionId { get; init; }

    public decimal DefaultWorkingHours { get; init; }
    public decimal OvertimeThreshold { get; init; }
    public int MaxWorkDays { get; init; }
    public int MinRestDays { get; init; }
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
