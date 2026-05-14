// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the SealedDay entity with query filter and
/// partial unique indexes that allow one global row and one row per group per date.
/// </summary>
using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class SealedDayConfiguration : IEntityTypeConfiguration<SealedDay>
{
    public void Configure(EntityTypeBuilder<SealedDay> builder)
    {
        builder.HasQueryFilter(s => !s.IsDeleted);

        builder.HasIndex(s => new { s.Date, s.GroupId })
            .HasDatabaseName("ix_sealed_day_date_group")
            .IsUnique()
            .HasFilter("\"group_id\" IS NOT NULL AND \"is_deleted\" = false");

        builder.HasIndex(s => s.Date)
            .HasDatabaseName("ix_sealed_day_date_global")
            .IsUnique()
            .HasFilter("\"group_id\" IS NULL AND \"is_deleted\" = false");
    }
}
