// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the FloorPlanWorkMarker-Entity with query filter.
/// </summary>
using Klacks.Api.Domain.Models.FloorPlans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class FloorPlanWorkMarkerConfiguration : IEntityTypeConfiguration<FloorPlanWorkMarker>
{
    public void Configure(EntityTypeBuilder<FloorPlanWorkMarker> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
