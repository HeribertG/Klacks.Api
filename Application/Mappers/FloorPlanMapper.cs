// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.FloorPlans;
using Klacks.Api.Domain.Models.FloorPlans;

namespace Klacks.Api.Application.Mappers;

public class FloorPlanMapper
{
    public FloorPlan ToFloorPlanEntity(FloorPlanResource resource)
    {
        return new FloorPlan
        {
            Id = resource.Id,
            Name = resource.Name,
            Description = resource.Description,
            CanvasJson = resource.CanvasJson,
            ThumbnailData = resource.ThumbnailData,
        };
    }

    public FloorPlanResource ToFloorPlanResource(FloorPlan entity)
    {
        return new FloorPlanResource
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            CanvasJson = entity.CanvasJson,
            ThumbnailData = entity.ThumbnailData,
            WorkMarkers = entity.WorkMarkers.Select(ToMarkerResource).ToList(),
        };
    }

    public FloorPlanWorkMarker ToMarkerEntity(FloorPlanWorkMarkerResource resource)
    {
        return new FloorPlanWorkMarker
        {
            Id = resource.Id,
            FloorPlanId = resource.FloorPlanId,
            WorkId = resource.WorkId,
            ClientId = resource.ClientId,
            Label = resource.Label,
            X = resource.X,
            Y = resource.Y,
            Width = resource.Width,
            Height = resource.Height,
            Color = resource.Color,
            MarkerType = resource.MarkerType,
        };
    }

    public FloorPlanWorkMarkerResource ToMarkerResource(FloorPlanWorkMarker entity)
    {
        return new FloorPlanWorkMarkerResource
        {
            Id = entity.Id,
            FloorPlanId = entity.FloorPlanId,
            WorkId = entity.WorkId,
            ClientId = entity.ClientId,
            Label = entity.Label,
            X = entity.X,
            Y = entity.Y,
            Width = entity.Width,
            Height = entity.Height,
            Color = entity.Color,
            MarkerType = entity.MarkerType,
        };
    }
}
