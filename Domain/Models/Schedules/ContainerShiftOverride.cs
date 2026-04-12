// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Day-specific override for a container shift, detached from the weekly template.
/// </summary>
/// <param name="ContainerId">FK to the container Shift entity</param>
/// <param name="Date">The specific date this override applies to</param>
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace Klacks.Api.Domain.Models.Schedules;

public class ContainerShiftOverride : BaseEntity
{
    [ForeignKey("Shift")]
    public Guid ContainerId { get; set; }

    public DateOnly Date { get; set; }

    public TimeOnly FromTime { get; set; }

    public TimeOnly UntilTime { get; set; }

    public string? StartBase { get; set; }

    public string? EndBase { get; set; }

    [Column(TypeName = "jsonb")]
    public RouteInfo? RouteInfo { get; set; }

    public ContainerTransportMode TransportMode { get; set; } = ContainerTransportMode.ByCar;

    public virtual Shift Shift { get; set; } = null!;

    public List<ContainerShiftOverrideItem> ContainerShiftOverrideItems { get; set; } = new List<ContainerShiftOverrideItem>();
}
