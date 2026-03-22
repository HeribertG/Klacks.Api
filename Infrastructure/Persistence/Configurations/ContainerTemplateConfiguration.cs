// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the ContainerTemplate-Entity with query filter, JSONB conversion, Index and Shift-relationship.
/// </summary>
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class ContainerTemplateConfiguration : IEntityTypeConfiguration<ContainerTemplate>
{
    public void Configure(EntityTypeBuilder<ContainerTemplate> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.Property(e => e.RouteInfo).HasJsonbConversion<RouteInfo>();
        builder.HasIndex(p => new { p.Id, p.ContainerId, p.Weekday, p.IsWeekdayAndHoliday, p.IsHoliday });

        builder.HasOne(ct => ct.Shift)
            .WithMany()
            .HasForeignKey(ct => ct.ContainerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
