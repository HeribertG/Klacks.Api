// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Entity for a single item in a ContainerTemplate (shift or absence).
/// </summary>
/// <param name="ShiftId">Optional: Reference to a Shift entity</param>
/// <param name="AbsenceId">Optional: Reference to an Absence entity</param>
/// <param name="StartItem">Positioned start time for all items (shifts + absences)</param>
/// <param name="EndItem">Positioned end time for all items (shifts + absences)</param>
/// <param name="TimeRangeStartItem">Flexible time window start (TimeRange shifts only)</param>
/// <param name="TimeRangeEndItem">Flexible time window end (TimeRange shifts only)</param>
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Models.Schedules;

public class ContainerTemplateItem : BaseEntity
{
    [ForeignKey("ContainerTemplate")]
    public Guid ContainerTemplateId { get; set; }

    [ForeignKey("Shift")]
    public Guid? ShiftId { get; set; }

    [ForeignKey("Absence")]
    public Guid? AbsenceId { get; set; }

    public TimeOnly? StartItem { get; set; }

    public TimeOnly? EndItem { get; set; }

    public TimeOnly BriefingTime { get; set; }

    public TimeOnly DebriefingTime { get; set; }

    public TimeOnly TravelTimeAfter { get; set; }

    public TimeOnly TravelTimeBefore { get; set; }

    public TimeOnly? TimeRangeStartItem { get; set; }

    public TimeOnly? TimeRangeEndItem { get; set; }

    public TransportMode TransportMode { get; set; } = TransportMode.ByCar;

    [JsonIgnore]
    public virtual ContainerTemplate ContainerTemplate { get; set; } = null!;

    [JsonIgnore]
    public virtual Shift? Shift { get; set; }

    [JsonIgnore]
    public virtual Absence? Absence { get; set; }
}
