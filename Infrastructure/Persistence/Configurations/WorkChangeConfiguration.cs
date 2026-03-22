// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the WorkChange-Entity with query filter and relationships to Work/ReplaceClient.
/// </summary>
using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class WorkChangeConfiguration : IEntityTypeConfiguration<WorkChange>
{
    public void Configure(EntityTypeBuilder<WorkChange> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => new { p.WorkId, p.IsDeleted });

        builder.HasOne(sc => sc.Work)
            .WithMany()
            .HasForeignKey(sc => sc.WorkId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sc => sc.ReplaceClient)
            .WithMany()
            .HasForeignKey(sc => sc.ReplaceClientId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
