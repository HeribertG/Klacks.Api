// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Single item (shift or absence) in a ContainerShiftOverride.
/// </summary>
/// <param name="ShiftId">Optional: Reference to a Shift entity (XOR with AbsenceId)</param>
/// <param name="AbsenceId">Optional: Reference to an Absence entity (XOR with ShiftId)</param>
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Models.Schedules;

public class ContainerShiftOverrideItem : BaseEntity
{
    [ForeignKey("ContainerShiftOverride")]
    public Guid ContainerShiftOverrideId { get; set; }

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
    public virtual ContainerShiftOverride ContainerShiftOverride { get; set; } = null!;

    [JsonIgnore]
    public virtual Shift? Shift { get; set; }

    [JsonIgnore]
    public virtual Absence? Absence { get; set; }
}
