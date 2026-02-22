// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules;

public class RouteSegmentDirectionsResource
{
    public string FromName { get; set; } = string.Empty;

    public string ToName { get; set; } = string.Empty;

    public string TransportMode { get; set; } = string.Empty;

    public double DistanceKm { get; set; }

    public string Duration { get; set; } = string.Empty;

    public List<DirectionStepResource> Steps { get; set; } = new List<DirectionStepResource>();
}
