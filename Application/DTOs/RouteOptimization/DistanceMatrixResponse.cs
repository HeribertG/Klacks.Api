// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.RouteOptimization;

public class DistanceMatrixResponse
{
    public List<LocationDto> Locations { get; set; } = new();
    public double[][] Matrix { get; set; } = Array.Empty<double[]>();
}
