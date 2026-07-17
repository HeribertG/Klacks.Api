// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for CompensatoryRestObligation (K12). The (ClientId, RestGapStart) pair is the
/// natural key the reconciler upserts by, enforced as a partial unique index over non-deleted rows so a
/// soft-deleted predecessor never collides with a re-created obligation for the same gap. A second
/// partial index over (ClientId, DueDate) for open (unfulfilled) rows serves the feed lookup.
/// RestGapStart holds a wall-clock schedule-block boundary (Unspecified kind in the non-DST path), so it
/// is mapped as timestamp without time zone rather than timestamptz.
/// </summary>

using Klacks.Api.Domain.Models.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class CompensatoryRestObligationConfiguration : IEntityTypeConfiguration<CompensatoryRestObligation>
{
    public void Configure(EntityTypeBuilder<CompensatoryRestObligation> builder)
    {
        builder.Property(o => o.RestGapStart).HasColumnType("timestamp without time zone");

        builder.HasIndex(o => new { o.ClientId, o.RestGapStart })
            .IsUnique()
            .HasFilter("is_deleted = false");

        builder.HasIndex(o => new { o.ClientId, o.DueDate })
            .HasFilter("is_deleted = false AND fulfilled_on IS NULL");
    }
}
