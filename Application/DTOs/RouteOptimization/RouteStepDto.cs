// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.RouteOptimization;

public class RouteStepDto : LocationDto
{
    public int Order { get; set; }
    public double DistanceToNextKm { get; set; }
    public TimeSpan TravelTimeToNext { get; set; }
}
