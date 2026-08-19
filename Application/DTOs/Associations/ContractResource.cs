// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json.Serialization;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.DTOs.Associations;

public class ContractResource
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal? GuaranteedHours { get; set; }

    public decimal MaximumHours { get; set; }

    public decimal MinimumHours { get; set; }

    public decimal FullTime { get; set; }

    public decimal NightRate { get; set; }

    public decimal HolidayRate { get; set; }

    [JsonPropertyName("we1Rate")]
    public decimal? WE1Rate { get; set; }

    [JsonPropertyName("we2Rate")]
    public decimal? WE2Rate { get; set; }

    [JsonPropertyName("we3Rate")]
    public decimal? WE3Rate { get; set; }

    public string? NightStart { get; set; }

    public string? NightEnd { get; set; }

    public PaymentInterval PaymentInterval { get; set; }

    public decimal? Percent { get; set; }

    public DateTime ValidFrom { get; set; }

    public DateTime? ValidUntil { get; set; }

    public CalendarSelectionResource? CalendarSelection { get; set; }

    public Guid? CalendarSelectionId { get; set; }

    public bool WorkOnMonday { get; set; } = true;

    public bool WorkOnTuesday { get; set; } = true;

    public bool WorkOnWednesday { get; set; } = true;

    public bool WorkOnThursday { get; set; } = true;

    public bool WorkOnFriday { get; set; } = true;

    public bool WorkOnSaturday { get; set; }

    public bool WorkOnSunday { get; set; }

    public bool PerformsShiftWork { get; set; }

    public Guid? SchedulingRuleId { get; set; }

    public Guid? IndividualPeriodId { get; set; }
}