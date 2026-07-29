// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.CalendarSelections;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Domain.Models.Schedules;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Models.Associations;

public class Contract : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public decimal? GuaranteedHours { get; set; }

    public decimal? MaximumHours { get; set; }

    public decimal? MinimumHours { get; set; }

    public decimal? FullTime { get; set; }

    public decimal? NightRate { get; set; }

    public decimal? HolidayRate { get; set; }

    public decimal? WE1Rate { get; set; }

    public decimal? WE2Rate { get; set; }

    public decimal? WE3Rate { get; set; }

    public string? NightStart { get; set; }

    public string? NightEnd { get; set; }

    public PaymentInterval PaymentInterval { get; set; } = PaymentInterval.Monthly;

    /// <summary>
    /// Workload share in percent, only meaningful when <see cref="PaymentInterval"/> is
    /// <see cref="PaymentInterval.MonthlyTargetHours"/>. Scales the company-wide monthly target
    /// hours down to this contract. Null is treated as 100 percent.
    /// </summary>
    public decimal? Percent { get; set; }

    /// <summary>
    /// Custom pay-period definition, only meaningful when <see cref="PaymentInterval"/> is
    /// <see cref="PaymentInterval.Individual"/>. Null for the fixed Weekly/Biweekly/Monthly cycles.
    /// </summary>
    [ForeignKey("IndividualPeriod")]
    public Guid? IndividualPeriodId { get; set; }

    [JsonIgnore]
    public IndividualPeriod? IndividualPeriod { get; set; }

    [Required]
    public DateTime ValidFrom { get; set; }

    public DateTime? ValidUntil { get; set; }

    [ForeignKey("CalendarSelection")]
    public Guid? CalendarSelectionId { get; set; }

    [JsonIgnore]
    public CalendarSelection? CalendarSelection { get; set; }

    public bool WorkOnMonday { get; set; } = true;

    public bool WorkOnTuesday { get; set; } = true;

    public bool WorkOnWednesday { get; set; } = true;

    public bool WorkOnThursday { get; set; } = true;

    public bool WorkOnFriday { get; set; } = true;

    public bool WorkOnSaturday { get; set; }

    public bool WorkOnSunday { get; set; }

    public bool PerformsShiftWork { get; set; }

    [ForeignKey("SchedulingRule")]
    public Guid? SchedulingRuleId { get; set; }

    [JsonIgnore]
    public SchedulingRule? SchedulingRule { get; set; }
}
