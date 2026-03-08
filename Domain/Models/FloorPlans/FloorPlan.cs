// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.ComponentModel.DataAnnotations;
using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.FloorPlans;

public class FloorPlan : BaseEntity
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? CanvasJson { get; set; }

    public string? ThumbnailData { get; set; }

    public virtual ICollection<FloorPlanWorkMarker> WorkMarkers { get; set; } = new List<FloorPlanWorkMarker>();
}
