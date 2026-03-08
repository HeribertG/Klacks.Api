// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.FloorPlans;

public class FloorPlanWorkMarker : BaseEntity
{
    public Guid FloorPlanId { get; set; }

    public virtual FloorPlan? FloorPlan { get; set; }

    public Guid? WorkId { get; set; }

    public Guid? ClientId { get; set; }

    public string? Label { get; set; }

    public double X { get; set; }

    public double Y { get; set; }

    public double Width { get; set; } = 120;

    public double Height { get; set; } = 80;

    public string? Color { get; set; }

    public FloorPlanMarkerType MarkerType { get; set; }
}
