// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Scheduling;

public class CounterRuleResource
{
    public Guid Id { get; set; }

    public CounterEventType EventType { get; set; }

    public CounterPeriod Period { get; set; }

    public int Threshold { get; set; }

    public decimal? HoursThreshold { get; set; }

    public RuleEnforcementMode? Enforcement { get; set; }

    public Guid? SchedulingRuleId { get; set; }
}
