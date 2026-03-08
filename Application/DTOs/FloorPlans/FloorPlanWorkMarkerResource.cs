// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.FloorPlans;

public class FloorPlanWorkMarkerResource
{
    public Guid Id { get; set; }

    public Guid FloorPlanId { get; set; }

    public Guid? WorkId { get; set; }

    public Guid? ClientId { get; set; }

    public string? Label { get; set; }

    public double X { get; set; }

    public double Y { get; set; }

    public double Width { get; set; }

    public double Height { get; set; }

    public string? Color { get; set; }

    public FloorPlanMarkerType MarkerType { get; set; }

    public string? ClientName { get; set; }

    public string? ShiftName { get; set; }

    public string? StartTime { get; set; }

    public string? EndTime { get; set; }
}
