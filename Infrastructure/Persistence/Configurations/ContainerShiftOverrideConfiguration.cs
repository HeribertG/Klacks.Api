// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for ContainerShiftOverride with query filter, JSONB conversion, unique constraint and Shift relationship.
/// </summary>
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class ContainerShiftOverrideConfiguration : IEntityTypeConfiguration<ContainerShiftOverride>
{
    public void Configure(EntityTypeBuilder<ContainerShiftOverride> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.Property(e => e.RouteInfo).HasJsonbConversion<RouteInfo>();
        builder.HasIndex(p => new { p.ContainerId, p.Date }).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne(o => o.Shift)
            .WithMany()
            .HasForeignKey(o => o.ContainerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
