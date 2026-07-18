// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Scheduling;

public class PeriodCapRuleResource
{
    public Guid Id { get; set; }

    public PeriodCapPeriod Period { get; set; }

    public PeriodCapScope Scope { get; set; }

    public decimal CapHours { get; set; }

    public int? WarnAtPercent { get; set; }

    public int? CustomPeriodWeeks { get; set; }

    public int? RollingWindowWeeks { get; set; }

    public decimal? MaxAverageWeeklyHours { get; set; }

    public Guid? SchedulingRuleId { get; set; }
}
