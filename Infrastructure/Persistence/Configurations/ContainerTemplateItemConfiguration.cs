// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core Konfiguration fuer die ContainerTemplateItem-Entity mit QueryFilter und ContainerTemplate-Beziehung.
/// </summary>
using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class ContainerTemplateItemConfiguration : IEntityTypeConfiguration<ContainerTemplateItem>
{
    public void Configure(EntityTypeBuilder<ContainerTemplateItem> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.HasOne(cti => cti.ContainerTemplate)
            .WithMany(ct => ct.ContainerTemplateItems)
            .HasForeignKey(cti => cti.ContainerTemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
