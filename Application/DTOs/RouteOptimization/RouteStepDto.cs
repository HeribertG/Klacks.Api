// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.RouteOptimization;

public class RouteStepDto : LocationDto
{
    public int Order { get; set; }
    public double DistanceToNextKm { get; set; }
    public TimeSpan TravelTimeToNext { get; set; }
}
