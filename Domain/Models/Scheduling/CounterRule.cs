// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// A generic per-person event-counter rule (K18): count <see cref="EventType"/> occurrences within the
/// calendar-anchored <see cref="Period"/> and report once the count reaches <see cref="Threshold"/>
/// (e.g. "25th night shift per year" CH, "6th worked day per week" GR, "more than 12 heavy-duty days
/// per month" AT). HoursThreshold parameterizes ShiftExceedingHours (e.g. 13-hour days). Stage 1 action
/// is warn/block via the counterRule enforcement mode; a surcharge-applying action is a documented later
/// stage. Scope: with <see cref="SchedulingRuleId"/> null the rule is GLOBAL; otherwise it applies only
/// to clients whose active contract references that scheduling rule (the industry axis). Rows are
/// written exclusively by the region-setup entity-import mechanism (K20);
/// ImportSourceKey/ImportContentHash (see <see cref="Klacks.Api.Domain.Common.IImportableEntity"/>)
/// drive that re-apply logic.
/// </summary>

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Scheduling;

public class CounterRule : BaseEntity, IImportableEntity
{
    public CounterEventType EventType { get; set; }

    public CounterPeriod Period { get; set; }

    public int Threshold { get; set; }

    public decimal? HoursThreshold { get; set; }

    public Guid? SchedulingRuleId { get; set; }

    public string ImportSourceKey { get; set; } = string.Empty;

    public string ImportContentHash { get; set; } = string.Empty;
}
