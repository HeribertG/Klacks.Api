// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the ExportFormatOverride entity with soft-delete query filter and a
/// partial unique index on FormatKey (one active override per format).
/// </summary>
using Klacks.Api.Domain.Models.Exports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class ExportFormatOverrideConfiguration : IEntityTypeConfiguration<ExportFormatOverride>
{
    public void Configure(EntityTypeBuilder<ExportFormatOverride> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => p.FormatKey)
            .IsUnique()
            .HasFilter("\"is_deleted\" = false");
    }
}
