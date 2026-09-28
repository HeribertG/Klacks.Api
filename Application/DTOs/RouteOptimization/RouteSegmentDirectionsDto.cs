// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.RouteOptimization;

public class RouteSegmentDirectionsDto
{
    public string FromName { get; set; } = string.Empty;
    public string ToName { get; set; } = string.Empty;
    public string TransportMode { get; set; } = string.Empty;
    public double DistanceKm { get; set; }
    public TimeSpan Duration { get; set; }
    public List<DirectionStepDto> Steps { get; set; } = new();
}
