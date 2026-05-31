// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for ClientQualification: soft-delete filter, a partial unique index so a
/// soft-deleted and an active (client, qualification) row can coexist, a time-versioned index for
/// validity-range lookups, and the Client / Qualification relationships.
/// </summary>

using Klacks.Api.Domain.Models.Associations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class ClientQualificationConfiguration : IEntityTypeConfiguration<ClientQualification>
{
    public void Configure(EntityTypeBuilder<ClientQualification> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => new { p.ClientId, p.ValidFrom, p.ValidUntil, p.IsDeleted });

        builder.HasIndex(p => new { p.ClientId, p.QualificationId })
            .IsUnique()
            .HasFilter("\"is_deleted\" = false");

        builder.HasOne(cq => cq.Client)
            .WithMany(c => c.Qualifications)
            .HasForeignKey(cq => cq.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cq => cq.Qualification)
            .WithMany()
            .HasForeignKey(cq => cq.QualificationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
