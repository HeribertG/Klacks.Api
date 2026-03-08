// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.FloorPlans;

public class FloorPlanResource
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? CanvasJson { get; set; }

    public string? ThumbnailData { get; set; }

    public List<FloorPlanWorkMarkerResource> WorkMarkers { get; set; } = new();
}
