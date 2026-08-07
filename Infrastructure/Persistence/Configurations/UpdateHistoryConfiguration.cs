// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the UpdateHistory entity: table name, soft-delete query filter and
/// query indexes. The single-active-operation partial unique index is created via raw SQL in the
/// migration because it filters on a constant expression EF cannot model. Status doubles as a
/// concurrency token: the out-of-process updater claims and completes rows through raw SQL, so any
/// in-process write that read the row first has to fail rather than overwrite a status the updater
/// changed in between. This only shapes the generated UPDATE, it is not a schema change.
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
        builder.Property(p => p.Status).IsConcurrencyToken();
        builder.HasIndex(p => p.Status).HasFilter("is_deleted = false");
        builder.HasIndex(p => p.RequestedAt);
    }
}
