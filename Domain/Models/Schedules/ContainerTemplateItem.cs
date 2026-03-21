// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Entity fuer ein einzelnes Item in einem ContainerTemplate (Shift oder Absence).
/// </summary>
/// <param name="ShiftId">Optional: Referenz auf eine Shift-Entity</param>
/// <param name="AbsenceId">Optional: Referenz auf eine Absence-Entity</param>
/// <param name="StartItem">Positionierte Startzeit fuer alle Items (Shifts + Absences)</param>
/// <param name="EndItem">Positionierte Endzeit fuer alle Items (Shifts + Absences)</param>
/// <param name="TimeRangeStartItem">Flexibles Zeitfenster Start (nur TimeRange-Shifts)</param>
/// <param name="TimeRangeEndItem">Flexibles Zeitfenster Ende (nur TimeRange-Shifts)</param>
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
