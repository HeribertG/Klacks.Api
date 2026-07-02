// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the ErpDropPoint entity with query filter and a partial unique index on the source system key.
/// </summary>
using Klacks.Api.Domain.Models.Imports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class ErpDropPointConfiguration : IEntityTypeConfiguration<ErpDropPoint>
{
    public void Configure(EntityTypeBuilder<ErpDropPoint> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.HasIndex(p => p.SourceSystemId)
            .IsUnique()
            .HasFilter("is_deleted = false");
    }
}
