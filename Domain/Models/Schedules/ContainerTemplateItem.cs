// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
    public Guid ShiftId { get; set; }

    public TimeOnly? StartShift { get; set; }

    public TimeOnly? EndShift { get; set; }

    public TimeOnly BriefingTime { get; set; }

    public TimeOnly DebriefingTime { get; set; }

    public TimeOnly TravelTimeAfter { get; set; }

    public TimeOnly TravelTimeBefore { get; set; }

    public TimeOnly? TimeRangeStartShift { get; set; }

    public TimeOnly? TimeRangeEndShift { get; set; }

    public TransportMode TransportMode { get; set; } = TransportMode.ByCar;

    [JsonIgnore]
    public virtual ContainerTemplate ContainerTemplate { get; set; } = null!;

    [JsonIgnore]
    public virtual Shift Shift { get; set; } = null!;
}
