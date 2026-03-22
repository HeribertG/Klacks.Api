// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// POST body for the optimize-route endpoint.
/// </summary>
/// <param name="ShiftIds">IDs of shifts to optimize</param>
/// <param name="TimeBlocks">Optional time blocks (breaks etc.)</param>
/// <param name="ContainerFromTime">Container start time (HH:mm) for correct time block placement</param>

namespace Klacks.Api.Application.DTOs.RouteOptimization;

public class OptimizeRouteRequestDto
{
    public List<Guid> ShiftIds { get; set; } = new();
    public List<TimeBlockDto> TimeBlocks { get; set; } = new();
    public string? ContainerFromTime { get; set; }
}
