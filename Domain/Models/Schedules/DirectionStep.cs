// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Schedules;

public class DirectionStep
{
    public string Instruction { get; set; } = string.Empty;

    public string StreetName { get; set; } = string.Empty;

    public double DistanceMeters { get; set; }

    public int DurationSeconds { get; set; }

    public string ManeuverType { get; set; } = string.Empty;
}
