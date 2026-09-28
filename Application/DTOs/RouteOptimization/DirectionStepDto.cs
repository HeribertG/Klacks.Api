// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.RouteOptimization;

public class DirectionStepDto
{
    public string Instruction { get; set; } = string.Empty;
    public string StreetName { get; set; } = string.Empty;
    public double DistanceMeters { get; set; }
    public int DurationSeconds { get; set; }
    public string ManeuverType { get; set; } = string.Empty;
}
