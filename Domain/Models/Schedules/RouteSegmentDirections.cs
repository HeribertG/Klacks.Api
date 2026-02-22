// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Schedules;

public class RouteSegmentDirections
{
    public string FromName { get; set; } = string.Empty;

    public string ToName { get; set; } = string.Empty;

    public string TransportMode { get; set; } = string.Empty;

    public double DistanceKm { get; set; }

    public string Duration { get; set; } = string.Empty;

    public List<DirectionStep> Steps { get; set; } = new List<DirectionStep>();
}
