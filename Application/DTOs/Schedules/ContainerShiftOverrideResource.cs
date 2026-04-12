// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// DTO for a day-specific container shift override.
/// </summary>
/// <param name="ContainerId">The container shift this override belongs to</param>
/// <param name="Date">The specific date of this override</param>
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Schedules;

public class ContainerShiftOverrideResource
{
    public Guid Id { get; set; }

    public Guid ContainerId { get; set; }

    public DateOnly Date { get; set; }

    public TimeOnly FromTime { get; set; }

    public TimeOnly UntilTime { get; set; }

    public string? StartBase { get; set; }

    public string? EndBase { get; set; }

    public RouteInfoResource? RouteInfo { get; set; }

    public ContainerTransportMode TransportMode { get; set; } = ContainerTransportMode.ByCar;

    public bool HasWork { get; set; }

    public ShiftResource? Shift { get; set; }

    public List<ContainerShiftOverrideItemResource> ContainerShiftOverrideItems { get; set; } = new();
}
