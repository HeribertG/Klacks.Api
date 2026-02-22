// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.RouteOptimization;

public class DistanceMatrixResponse
{
    public List<LocationDto> Locations { get; set; } = new();
    public double[][] Matrix { get; set; } = Array.Empty<double[]>();
}
