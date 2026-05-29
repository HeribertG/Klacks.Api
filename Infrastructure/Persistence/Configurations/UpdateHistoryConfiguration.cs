// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the UpdateHistory entity: table name, soft-delete query filter and
/// query indexes. The single-active-operation partial unique index is created via raw SQL in the
/// migration because it filters on a constant expression EF cannot model.
/// </summary>
using Klacks.Api.Domain.Models.Update;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class UpdateHistoryConfiguration : IEntityTypeConfiguration<UpdateHistory>
{
    public void Configure(EntityTypeBuilder<UpdateHistory> builder)
    {
        builder.ToTable("update_history");
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => p.Status).HasFilter("is_deleted = false");
        builder.HasIndex(p => p.RequestedAt);
    }
}
