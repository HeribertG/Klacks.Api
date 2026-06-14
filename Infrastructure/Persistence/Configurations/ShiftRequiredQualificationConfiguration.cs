// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for ShiftRequiredQualification: soft-delete filter, a partial unique index
/// so a soft-deleted and an active (shift, qualification) row can coexist, and the Shift /
/// Qualification relationships.
/// </summary>

using Klacks.Api.Domain.Models.Associations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class ShiftRequiredQualificationConfiguration : IEntityTypeConfiguration<ShiftRequiredQualification>
{
    public void Configure(EntityTypeBuilder<ShiftRequiredQualification> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => new { p.ShiftId, p.IsDeleted });

        builder.HasIndex(p => new { p.ShiftId, p.QualificationId })
            .IsUnique()
            .HasFilter("\"is_deleted\" = false");

        builder.HasOne(srq => srq.Shift)
            .WithMany(s => s.RequiredQualifications)
            .HasForeignKey(srq => srq.ShiftId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(srq => srq.Qualification)
            .WithMany()
            .HasForeignKey(srq => srq.QualificationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
