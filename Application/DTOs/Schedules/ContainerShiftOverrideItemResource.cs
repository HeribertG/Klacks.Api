// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// DTO for a single item in a ContainerShiftOverride.
/// </summary>
/// <param name="ShiftId">Optional: Shift reference (XOR with AbsenceId)</param>
/// <param name="AbsenceId">Optional: Absence reference (XOR with ShiftId)</param>
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Schedules;

public class ContainerShiftOverrideItemResource
{
    public Guid Id { get; set; }

    public Guid ContainerShiftOverrideId { get; set; }

    public Guid? ShiftId { get; set; }

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

    public ShiftResource? Shift { get; set; }

    public AbsenceResource? Absence { get; set; }
}
