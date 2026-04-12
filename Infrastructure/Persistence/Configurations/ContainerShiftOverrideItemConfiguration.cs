// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for ContainerShiftOverrideItem with query filter, relationships and XOR check constraint.
/// </summary>
using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class ContainerShiftOverrideItemConfiguration : IEntityTypeConfiguration<ContainerShiftOverrideItem>
{
    public void Configure(EntityTypeBuilder<ContainerShiftOverrideItem> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.HasOne(i => i.ContainerShiftOverride)
            .WithMany(o => o.ContainerShiftOverrideItems)
            .HasForeignKey(i => i.ContainerShiftOverrideId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Shift)
            .WithMany()
            .HasForeignKey(i => i.ShiftId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Absence)
            .WithMany()
            .HasForeignKey(i => i.AbsenceId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_ContainerShiftOverrideItem_ShiftXorAbsence",
            "(shift_id IS NOT NULL AND absence_id IS NULL) OR (shift_id IS NULL AND absence_id IS NOT NULL)"));
    }
}
